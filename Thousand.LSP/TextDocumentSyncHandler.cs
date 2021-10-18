using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;
using System.Threading;
using System.Threading.Tasks;

namespace Thousand.LSP
{
    class TextDocumentSyncHandler : TextDocumentSyncHandlerBase
    {
        private readonly ILogger<TextDocumentSyncHandler> logger;
        private readonly ILanguageServerConfiguration configuration;
        private readonly BufferService documentService;
        private readonly AnalysisService semanticService;
        private readonly IDiagnosticService diagnosticService;
        private readonly IGenerationService generationService;

        public TextDocumentSyncHandler(ILogger<TextDocumentSyncHandler> logger, ILanguageServerConfiguration configuration, BufferService documentService, AnalysisService semanticService, IDiagnosticService diagnosticService, IGenerationService generationService)
        {
            this.logger = logger;
            this.configuration = configuration;
            this.documentService = documentService;
            this.semanticService = semanticService;
            this.diagnosticService = diagnosticService;
            this.generationService = generationService;
        }

        protected override TextDocumentSyncRegistrationOptions CreateRegistrationOptions(SynchronizationCapability capability, ClientCapabilities clientCapabilities) => new()
        {
            DocumentSelector = DocumentSelector.ForLanguage("thousand"),
            Change = TextDocumentSyncKind.Incremental,
            Save = new SaveOptions { IncludeText = false } // we don't need it for anything
        };

        public override TextDocumentAttributes GetTextDocumentAttributes(DocumentUri uri) => new(uri, "thousand");

        public override async Task<Unit> Handle(DidOpenTextDocumentParams request, CancellationToken cancellationToken)
        {
            var config = await configuration.GetScopedConfiguration(request.TextDocument.Uri, cancellationToken);
            var options = new ServerOptions();
            config.GetSection("thousand").GetSection("server").Bind(options);

            documentService.Add(request.TextDocument.Uri, request.TextDocument.Text);
            if (options.PreviewDiagrams) generationService.Track(request.TextDocument.Uri);
            diagnosticService.Track(request.TextDocument.Uri);
            semanticService.Reparse(request.TextDocument.Uri);

            return Unit.Value;
        }

        public override Task<Unit> Handle(DidCloseTextDocumentParams request, CancellationToken cancellationToken)
        {
            if (configuration.TryGetScopedConfiguration(request.TextDocument.Uri, out var disposable))
            {
                disposable.Dispose();
            }

            generationService.Untrack(request.TextDocument.Uri);
            diagnosticService.Untrack(request.TextDocument.Uri);
            documentService.Remove(request.TextDocument.Uri);            

            return Unit.Task;
        }

        public override Task<Unit> Handle(DidChangeTextDocumentParams request, CancellationToken cancellationToken)
        {
            foreach (var change in request.ContentChanges)
            {
                if (change.Range != null)
                {
                    documentService.ApplyIncrementalChange(request.TextDocument.Uri, change.Range, change.Text);
                }
                else
                {
                    documentService.ApplyFullChange(request.TextDocument.Uri, change.Text);
                }
            }

            semanticService.Reparse(request.TextDocument.Uri);

            return Unit.Task;
        }

        public override Task<Unit> Handle(DidSaveTextDocumentParams request, CancellationToken cancellationToken)
        {
            return Unit.Task;
        }
    }
}
