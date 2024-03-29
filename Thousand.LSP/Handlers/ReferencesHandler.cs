﻿using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Thousand.LSP.Analyse;

namespace Thousand.LSP.Handlers
{
    class ReferencesHandler : ReferencesHandlerBase
    {
        private readonly AnalysisService analysisService;

        public ReferencesHandler(AnalysisService analysisService)
        {
            this.analysisService = analysisService;
        }

        protected override ReferenceRegistrationOptions CreateRegistrationOptions(ReferenceCapability capability, ClientCapabilities clientCapabilities) => new ReferenceRegistrationOptions
        {
            DocumentSelector = DocumentSelector.ForLanguage("thousand")
        };

        public override async Task<LocationContainer> Handle(ReferenceParams request, CancellationToken cancellationToken)
        {
            var analysis = await analysisService.GetAnalysisAsync(request.TextDocument.Uri);

            foreach (var (uri, loc, ast) in analysis.ObjectReferences)
            {
                if (uri == request.TextDocument.Uri && loc.Contains(request.Position))
                {
                    var refs = analysis.ObjectReferences
                        .Where(r => ReferenceEquals(r.Value, ast))
                        .Select(r => new Location { Uri = r.Uri, Range = r.Range });

                    return new LocationContainer(refs);
                }
            }

            foreach (var (uri, loc, ast) in analysis.ClassReferences)
            {
                if (uri == request.TextDocument.Uri && loc.Contains(request.Position))
                {
                    var refs = analysis.ClassReferences
                        .Where(r => ReferenceEquals(r.Value, ast))
                        .Select(r => new Location { Uri = r.Uri, Range = r.Range });

                    return new LocationContainer(refs);
                }
            }

            return new LocationContainer();
        }
    }
}
