﻿using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System.Threading;
using System.Threading.Tasks;
using Thousand.Parse;

namespace Thousand.LSP
{
    class HoverHandler : HoverHandlerBase
    {
        private readonly AnalysisService semanticService;
        private readonly IDiagnosticService diagnosticService;

        public HoverHandler(AnalysisService semanticService, IDiagnosticService diagnosticService)
        {
            this.semanticService = semanticService;
            this.diagnosticService = diagnosticService;
        }

        protected override HoverRegistrationOptions CreateRegistrationOptions(HoverCapability capability, ClientCapabilities clientCapabilities) => new HoverRegistrationOptions
        {
            DocumentSelector = DocumentSelector.ForLanguage("thousand")
        };

        public override async Task<Hover?> Handle(HoverParams request, CancellationToken cancellationToken)
        {
            if (diagnosticService.HasDiagnostics(request.TextDocument.Uri, request.Position))
            {
                return null;
            }

            var analysis = await semanticService.GetAnalysisAsync(request.TextDocument.Uri);

            foreach (var (loc, ast) in analysis.ObjectReferences)
            {
                var r = loc.AsRange();
                if (r.Contains(request.Position))
                {
                    var tooltip = Format.Canonicalise(ast);

                    return new Hover
                    {
                        Range = r,
                        Contents = Format.CodeBlock(tooltip)
                    };
                }
            }

            foreach (var (loc, ast) in analysis.ClassReferences)
            {
                var r = loc.AsRange();
                if (r.Contains(request.Position))
                {
                    var tooltip = Format.Canonicalise(ast);

                    return new Hover
                    {
                        Range = r,
                        Contents = Format.CodeBlock(tooltip)
                    };
                }
            }

            return null;
        }
    }
}
