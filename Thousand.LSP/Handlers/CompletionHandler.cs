using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Thousand.LSP.Analyse;
using Thousand.Parse.Attributes;

namespace Thousand.LSP.Handlers
{
    class CompletionHandler : CompletionHandlerBase
    {
        private readonly ILogger<CompletionHandler> logger;
        private readonly AnalysisService analysisService;
        private readonly API metadata;

        public CompletionHandler(ILogger<CompletionHandler> logger, AnalysisService analysisService)
        {
            this.logger = logger;
            this.analysisService = analysisService;
            this.metadata = new API();
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
                    var names = attribute.ParentKind switch
                    {
                        ParentKind.Document => metadata.DocumentNames,
                        ParentKind.Class => metadata.ClassNames, // XXX this could be narrowed by existing attributes
                        ParentKind.Object => metadata.ObjectNames,
                        ParentKind.Line => metadata.LineNames,
                    };

                    if (!names.Any(name => attribute.ParentAttributes.Contains(name)))
                    {
                        foreach (var name in names)
                        {
                            logger.LogInformation($"{name} doc: {metadata.Documentation.ContainsKey(name)}");
                            completions.Add(new CompletionItem
                            {
                                Kind = CompletionItemKind.Enum,
                                Label = name,
                                InsertText = attribute.Syntax.HasEqualsSign ? name : $"{name}=",
                                Documentation = metadata.Documentation.ContainsKey(name) ? new MarkupContent { Kind = MarkupKind.Markdown, Value = metadata.Documentation[name] } : null
                            });
                        }
                    }
                }
            }

            return new CompletionList(completions);
        }
    }
}
