using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Thousand.LSP.Analyse;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Thousand.LSP.Handlers
{
    public class CompletionHandler : CompletionHandlerBase
    {
        private readonly ILogger<CompletionHandler> logger;
        private readonly AnalysisService analysisService;
        private readonly API.Metadata api;
        private readonly Container<string> commitAttribute = new[] { " ", "=", "]" };
        private readonly Container<string> commitClass = new[] { " ", ";", ".", "(" };
        private readonly Container<string> commitKeyword = new[] { " " };
        private readonly Container<string> commitObject = new[] { " ", "-", "|" };

        public CompletionHandler(ILogger<CompletionHandler> logger, API.Metadata api, AnalysisService analysisService)
        {
            this.logger = logger;
            this.analysisService = analysisService;
            this.api = api;
        }

        protected override CompletionRegistrationOptions CreateRegistrationOptions(CompletionCapability capability, ClientCapabilities clientCapabilities) => new CompletionRegistrationOptions()
        {
            DocumentSelector = DocumentSelector.ForLanguage("thousand"),
            TriggerCharacters = new[] { "[", ",", "." }
        };

        public override Task<CompletionItem> Handle(CompletionItem request, CancellationToken cancellationToken)
        {
            return Task.FromResult(request);
        }

        public override async Task<CompletionList> Handle(CompletionParams request, CancellationToken cancellationToken)
        {
            var analysis = await analysisService.GetAnalysisAsync(request.TextDocument.Uri);
            return GenerateCompletions(analysis, request.Position);
        }

        public CompletionList GenerateCompletions(Analysis analysis, Position position)
        {
            var completions = new List<CompletionItem>();

            if (analysis.Main != null)
            {
                for (var i = 0; i < analysis.Main.Attributes.Count; i++)
                {
                    if (analysis.Main.Attributes[i].Range.Contains(position))
                    {
                        var syntax = analysis.Main.Attributes[i].Syntax;

                        var candidates = analysis.Main.Attributes[i].ParentKind switch
                        {
                            ParentKind.Class => api.ClassAttributes.AsEnumerable(),
                            ParentKind.Document => api.EntityDefinitions,
                            ParentKind.Object => api.ObjectDefinitions,
                            ParentKind.Line => api.LineDefinitions,
                        };

                        foreach (var candidate in candidates)
                        {
                            if (syntax.Key == null || 
                                candidate.Names.Contains(syntax.Key.Text, StringComparer.OrdinalIgnoreCase) || 
                                !candidate.Names.Any(name => analysis.Main.Attributes[i].ParentAttributes.Contains(name)))
                            {
                                foreach (var name in candidate.Names)
                                {
                                    logger.LogInformation($"{name} doc: {api.Documentation.ContainsKey(name)}");
                                    completions.Add(new CompletionItem
                                    {
                                        Kind = CompletionItemKind.Enum,
                                        Label = name,
                                        InsertText = syntax.HasEqualsSign ? name : $"{name}=",
                                        Documentation = candidate.Documentation != null ? new MarkupContent { Kind = MarkupKind.Markdown, Value = candidate.Documentation } : null,
                                        CommitCharacters = commitAttribute
                                    });
                                }
                            }
                        }

                        break;
                    }
                }

                for (var i = 0; i < analysis.Main.ClassNames.Count; i++)
                {
                    if (analysis.Main.ClassNames[i].Range.Contains(position))
                    {
                        var existing = analysis.Main.ClassNames[i].Text;
                        var candidates = analysis.ClassDefinitions.Keys;
                        var scope = analysis.Main.ClassNames[i].Scope;

                        foreach (var candidate in candidates.Where(c => scope.FindClass(c.Name) != null))
                        {
                            var completion = candidate.Name.Text;
                            var existingStart = existing.IndexOf(completion[0]);
                            var existingLength = existingStart == -1 ? 0 : existing.Length - existingStart;
                            var location = new Range(position.Delta(0, -existingLength), position);

                            logger.LogInformation($"suggesting completion: {completion}@{location}");

                            completions.Add(new CompletionItem
                            {
                                Kind = CompletionItemKind.Class,
                                Label = candidate.Name.Text,
                                TextEdit = new TextEdit { NewText = completion, Range = location },
                                Documentation = Format.CodeBlock(Format.Canonicalise(candidate)),
                                CommitCharacters = commitClass
                            });
                        }

                        if (analysis.Main.ClassNames[i].IsAtStart)
                        {
                            completions.Add(new CompletionItem
                            {
                                Kind = CompletionItemKind.Keyword,
                                Label = "class",
                                CommitCharacters = commitKeyword
                            });
                        }

                        break;
                    }
                }

                for (var i = 0; i < analysis.Main.ObjectNames.Count; i++)
                {
                    if (analysis.Main.ObjectNames[i].Range.Contains(position))
                    {
                        var existing = analysis.Main.ObjectNames[i].Text;
                        var candidates = analysis.ObjectDefinitions.Keys;
                        var scope = analysis.Main.ObjectNames[i].Scope;

                        foreach (var candidate in candidates.Where(c => c.Name != null && scope.FindObject(c.Name) != null))
                        {
                            var completion = candidate.Name!.Text;
                            var existingStart = existing.IndexOf(completion[0]);
                            var existingLength = existingStart == -1 ? 0 : existing.Length - existingStart;
                            var location = new Range(position.Delta(0, -existingLength), position);

                            logger.LogInformation($"suggesting completion: {completion}@{location}");

                            completions.Add(new CompletionItem
                            {
                                Kind = CompletionItemKind.Variable,
                                Label = candidate.Name.Text,
                                TextEdit = new TextEdit { NewText = completion, Range = location },
                                Documentation = Format.CodeBlock(Format.Canonicalise(candidate)),
                                CommitCharacters = commitObject
                            });
                        }

                        break;
                    }
                }
            }

            return new CompletionList(completions);
        }
    }
}
