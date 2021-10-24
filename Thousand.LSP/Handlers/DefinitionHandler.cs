using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System.Threading;
using System.Threading.Tasks;
using Thousand.LSP.Analyse;

namespace Thousand.LSP.Handlers
{
    class DefinitionHandler : DefinitionHandlerBase
    {
        private readonly AnalysisService analysisService;

        public DefinitionHandler(AnalysisService analysisService)
        {
            this.analysisService = analysisService;
        }

        protected override DefinitionRegistrationOptions CreateRegistrationOptions(DefinitionCapability capability, ClientCapabilities clientCapabilities) => new DefinitionRegistrationOptions
        {
            DocumentSelector = DocumentSelector.ForLanguage("thousand")
        };

        public override async Task<LocationOrLocationLinks> Handle(DefinitionParams request, CancellationToken cancellationToken)
        {
            var analysis = await analysisService.GetAnalysisAsync(request.TextDocument.Uri);

            foreach (var (uri, range, ast) in analysis.ObjectReferences)
            {
                if (uri == request.TextDocument.Uri && range.Contains(request.Position) && analysis.ObjectDefinitions.ContainsKey(ast))
                {
                    var def = new LocationOrLocationLink(analysis.ObjectDefinitions[ast]);
                    return new LocationOrLocationLinks(def);
                }
            }

            foreach (var (uri, range, ast) in analysis.ClassReferences)
            {
                if (uri == request.TextDocument.Uri && ast is not null && range.Contains(request.Position) && analysis.ClassDefinitions.ContainsKey(ast))
                {
                    var def = new LocationOrLocationLink(analysis.ClassDefinitions[ast]);
                    return new LocationOrLocationLinks(def);
                }
            }

            return new LocationOrLocationLinks();
        }
    }
}
