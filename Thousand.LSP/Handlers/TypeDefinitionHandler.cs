using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Thousand.LSP.Analyse;

namespace Thousand.LSP.Handlers
{
    class TypeDefinitionHandler : TypeDefinitionHandlerBase
    {
        private readonly AnalysisService analysisService;

        public TypeDefinitionHandler(AnalysisService analysisService)
        {
            this.analysisService = analysisService;
        }

        protected override TypeDefinitionRegistrationOptions CreateRegistrationOptions(TypeDefinitionCapability capability, ClientCapabilities clientCapabilities) => new TypeDefinitionRegistrationOptions
        {
            DocumentSelector = DocumentSelector.ForLanguage("thousand")
        };

        public override async Task<LocationOrLocationLinks> Handle(TypeDefinitionParams request, CancellationToken cancellationToken)
        {
            var analysis = await analysisService.GetAnalysisAsync(request.TextDocument.Uri);

            foreach (var (uri, range, ast) in analysis.ObjectReferences)
            {
                if (range.Contains(request.Position))
                {
                    var defs = analysis.ObjectClasses[ast]
                        .Select(classAST => analysis.ClassDefinitions[classAST])
                        .Select(loc => new LocationOrLocationLink(loc))
                        .ToArray();

                    return new LocationOrLocationLinks(defs);
                }
            }

            foreach (var (uri, range, ast) in analysis.ClassReferences)
            {
                if (ast is not null && range.Contains(request.Position))
                {
                    var defs = analysis.ClassClasses[ast]
                        .Select(classAST => analysis.ClassDefinitions[classAST])
                        .Select(loc => new LocationOrLocationLink(loc))
                        .ToArray();

                    return new LocationOrLocationLinks(defs);
                }
            }

            return new LocationOrLocationLinks();
        }
    }
}
