using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Thousand.LSP.Analyse;

namespace Thousand.LSP.Handlers
{
    class DocumentSymbolHandler : DocumentSymbolHandlerBase
    {
        private readonly AnalysisService analysisService;

        public DocumentSymbolHandler(AnalysisService analysisService)
        {
            this.analysisService = analysisService;
        }

        protected override DocumentSymbolRegistrationOptions CreateRegistrationOptions(DocumentSymbolCapability capability, ClientCapabilities clientCapabilities) => new DocumentSymbolRegistrationOptions
        {
            DocumentSelector = DocumentSelector.ForLanguage("thousand"),
            Label = "Thousand Words"
        };

        public override async Task<SymbolInformationOrDocumentSymbolContainer> Handle(DocumentSymbolParams request, CancellationToken cancellationToken)
        {
            var analysis = await analysisService.GetAnalysisAsync(request.TextDocument.Uri);

            return new SymbolInformationOrDocumentSymbolContainer(ReadSymbols(request.TextDocument.Uri, analysis));
        }

        private IEnumerable<SymbolInformationOrDocumentSymbol> ReadSymbols(DocumentUri uri, Analysis analysis)
        {
            if (analysis.Main != null)
            {
                foreach (var symbol in analysis.Main.Symbols)
                {
                    yield return symbol;
                }
            }
        }
    }
}
