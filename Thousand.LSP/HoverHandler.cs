using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
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

            var parse = await semanticService.GetAnalysisAsync(request.TextDocument.Uri);

            if (parse.FindDeclaration(request.Position) is Macro<AST.TolerantDocumentContent> macro)
            {
                if (macro.Value.IsT2)
                {
                    var tooltip = Format.Canonicalise(macro.Value.AsT2);

                    return new Hover
                    {
                        Range = macro.Span().AsRange(),
                        Contents = Format.CodeBlock(tooltip)
                    };
                }
                else if (macro.Value.IsT3)
                {
                    var tooltip = Format.Canonicalise(macro.Value.AsT3);

                    return new Hover
                    {
                        Range = macro.Span().AsRange(),
                        Contents = Format.CodeBlock(tooltip)
                    };
                }
                else if (macro.Value.IsT4)
                {
                    var tooltip = Format.Canonicalise(macro.Value.AsT4);

                    return new Hover
                    {
                        Range = macro.Span().AsRange(),
                        Contents = Format.CodeBlock(tooltip)
                    };
                }
            }

            return null;
        }
    }
}
