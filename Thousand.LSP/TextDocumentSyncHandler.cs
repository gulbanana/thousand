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

        public TextDocumentSyncHandler(ILogger<TextDocumentSyncHandler> logger, ILanguageServerConfiguration configuration)
        {
            this.logger = logger;
            this.configuration = configuration;            
        }

        protected override TextDocumentSyncRegistrationOptions CreateRegistrationOptions(SynchronizationCapability capability, ClientCapabilities clientCapabilities) => new()
        {
            DocumentSelector = new(
                new DocumentFilter { Language = "thousand" }
            ),
            Change = TextDocumentSyncKind.Full, // XXX 
            Save = new SaveOptions { IncludeText = false } // we don't need it for anything
        };

        public override TextDocumentAttributes GetTextDocumentAttributes(DocumentUri uri) => new(uri, "thousand");

        public override async Task<Unit> Handle(DidOpenTextDocumentParams request, CancellationToken cancellationToken)
        {
            logger.LogInformation("DidOpenTextDocument");

            var config = await configuration.GetScopedConfiguration(request.TextDocument.Uri, cancellationToken);
            var options = new ServerOptions();
            config.GetSection("thousand").GetSection("server").Bind(options);

            logger.LogInformation("Dummy: {0}", options.Dummy);

            return Unit.Value;
        }

        public override Task<Unit> Handle(DidCloseTextDocumentParams request, CancellationToken cancellationToken)
        {
            logger.LogInformation("DidCloseTextDocument");

            if (configuration.TryGetScopedConfiguration(request.TextDocument.Uri, out var disposable))
            {
                disposable.Dispose();
            }

            return Unit.Task;
        }

        public override Task<Unit> Handle(DidChangeTextDocumentParams request, CancellationToken cancellationToken)
        {
            logger.LogInformation("DidChangeTextDocument");

            return Unit.Task;
        }

        public override Task<Unit> Handle(DidSaveTextDocumentParams request, CancellationToken cancellationToken)
        {
            logger.LogInformation("DidSaveTextDocument");

            return Unit.Task;
        }
    }
}
