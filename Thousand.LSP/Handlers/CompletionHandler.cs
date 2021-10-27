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

namespace Thousand.LSP.Handlers
{
    class CompletionHandler : CompletionHandlerBase
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
            TriggerCharacters = new[] { "[", "," }
        };

        public override Task<CompletionItem> Handle(CompletionItem request, CancellationToken cancellationToken)
        {
            return Task.FromResult(request);
        }

        public override async Task<CompletionList> Handle(CompletionParams request, CancellationToken cancellationToken)
        {
            var completions = new List<CompletionItem>();

            var analysis = await analysisService.GetAnalysisAsync(request.TextDocument.Uri);
            if (analysis.Main != null)
            {
                var attribute = analysis.Main.Attributes.FirstOrDefault(a => a.Syntax.Key.Span.AsRange().Contains(request.Position));
                if (attribute != null)
                {
                    var candidates = attribute.ParentKind switch
                    {
                        ParentKind.Class => api.ClassAttributes.AsEnumerable(),
                        ParentKind.Document => api.EntityDefinitions,
                        ParentKind.Object => api.ObjectDefinitions,
                        ParentKind.Line => api.LineDefinitions,
                    };

                    foreach (var candidate in candidates)
                    {
                        if (candidate.Names.Contains(attribute.Syntax.Key.Text, StringComparer.OrdinalIgnoreCase) || !candidate.Names.Any(name => attribute.ParentAttributes.Contains(name)))
                        {
                            foreach (var name in candidate.Names)
                            {
                                logger.LogInformation($"{name} doc: {api.Documentation.ContainsKey(name)}");
                                completions.Add(new CompletionItem
                                {
                                    Kind = CompletionItemKind.Enum,
                                    Label = name,
                                    InsertText = attribute.Syntax.HasEqualsSign ? name : $"{name}=",
                                    Documentation = candidate.Documentation != null ? new MarkupContent { Kind = MarkupKind.Markdown, Value = candidate.Documentation } : null
                                });
                            }
                        }
                    }
                }
            }

            return new CompletionList(completions);
        }
    }
}
