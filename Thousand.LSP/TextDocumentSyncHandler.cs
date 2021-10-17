using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Thousand.LSP
{
    class TextDocumentSyncHandler : TextDocumentSyncHandlerBase
    {
        private readonly ILogger<TextDocumentSyncHandler> logger;
        private readonly ILanguageServerConfiguration configuration;
        private readonly DocumentService documentService;

        public TextDocumentSyncHandler(ILogger<TextDocumentSyncHandler> logger, ILanguageServerConfiguration configuration, DocumentService documentService)
        {
            this.logger = logger;
            this.configuration = configuration;
            this.documentService = documentService;
        }

        protected override TextDocumentSyncRegistrationOptions CreateRegistrationOptions(SynchronizationCapability capability, ClientCapabilities clientCapabilities) => new()
        {
            DocumentSelector = new(
                new DocumentFilter { Language = "thousand" }
            ),
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

            return Unit.Value;
        }

        public override Task<Unit> Handle(DidCloseTextDocumentParams request, CancellationToken cancellationToken)
        {
            if (configuration.TryGetScopedConfiguration(request.TextDocument.Uri, out var disposable))
            {
                disposable.Dispose();
            }

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

            return Unit.Task;
        }

        public override Task<Unit> Handle(DidSaveTextDocumentParams request, CancellationToken cancellationToken)
        {
            return Unit.Task;
        }
    }
}
