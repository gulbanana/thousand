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
                var aCtx = analysis.Main.Attributes.FirstOrDefault(a => a.KeySpan.AsRange().Contains(position));
                if (aCtx != null)
                {
                    var candidates = aCtx.ParentKind switch
                    {
                        ParentKind.Class => api.ClassAttributes.AsEnumerable(),
                        ParentKind.Document => api.EntityDefinitions,
                        ParentKind.Object => api.ObjectDefinitions,
                        ParentKind.Line => api.LineDefinitions,
                    };

                    foreach (var candidate in candidates)
                    {
                        if (candidate.Names.Contains(aCtx.KeySpan.ToStringValue(), StringComparer.OrdinalIgnoreCase) || !candidate.Names.Any(name => aCtx.ParentAttributes.Contains(name)))
                        {
                            foreach (var name in candidate.Names)
                            {
                                logger.LogInformation($"{name} doc: {api.Documentation.ContainsKey(name)}");
                                completions.Add(new CompletionItem
                                {
                                    Kind = CompletionItemKind.Enum,
                                    Label = name,
                                    InsertText = aCtx.Syntax.HasEqualsSign ? name : $"{name}=",
                                    Documentation = candidate.Documentation != null ? new MarkupContent { Kind = MarkupKind.Markdown, Value = candidate.Documentation } : null,
                                    CommitCharacters = new[] { " ", "=", "]" }
                                });
                            }
                        }
                    }
                }

                var cCtx = analysis.Main.ClassNames.FirstOrDefault(c => c.Location.Contains(position));
                if (cCtx != null)
                {
                    var candidates = analysis.ClassDefinitions.Keys;
                    foreach (var candidate in candidates.Where(c => cCtx.Scope.FindClass(c.Name) != null))
                    {
                        var completion = candidate.Name.Text;
                        var existing = cCtx.Span.ToStringValue();
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
                            CommitCharacters = new[] { " ", ";", ".", "(" }
                        });
                    }
                }
            }

            return new CompletionList(completions);
        }
    }
}
