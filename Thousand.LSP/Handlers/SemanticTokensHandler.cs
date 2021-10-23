using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Thousand.LSP.Analyse;

namespace Thousand.LSP.Handlers
{
    class SemanticTokensHandler : SemanticTokensHandlerBase
    {
        private readonly ILogger<SemanticTokensHandler> logger;
        private readonly AnalysisService semanticService;

        public SemanticTokensHandler(ILogger<SemanticTokensHandler> logger, AnalysisService semanticService)
        {
            this.logger = logger;
            this.semanticService = semanticService;
        }

        protected override SemanticTokensRegistrationOptions CreateRegistrationOptions(SemanticTokensCapability capability, ClientCapabilities clientCapabilities) => new()
        {
            DocumentSelector = DocumentSelector.ForLanguage("thousand"),
            Full = new SemanticTokensCapabilityRequestFull() { Delta = true },
            Range = true,
            Legend = new ()
            {
                TokenTypes = new[] { SemanticTokenType.Class, SemanticTokenType.EnumMember }
            }
        };

        protected override Task<SemanticTokensDocument> GetSemanticTokensDocument(ITextDocumentIdentifierParams @params, CancellationToken cancellationToken)
        {
            return Task.FromResult(new SemanticTokensDocument(RegistrationOptions.Legend));
        }

        protected override async Task Tokenize(SemanticTokensBuilder builder, ITextDocumentIdentifierParams identifier, CancellationToken cancellationToken)
        {
            var document = await semanticService.GetAnalysisAsync(identifier.TextDocument.Uri);
            
            // despite what the API implies, deltas may only be positive
            var builderBuilder = new List<(Range r, int t)>();
            
            foreach (var (uri, cref, _) in document.ClassReferences)
            {
                if (uri == identifier.TextDocument.Uri)
                {
                    builderBuilder.Add((cref, 0));
                }
            }

            foreach (var attribute in document.Attributes)
            {
                foreach (var token in attribute.Value.Sequence())
                {
                    if (token.Kind == Parse.TokenKind.Identifier)
                    {
                        builderBuilder.Add((token.Span.AsRange(), 1));
                    }
                }
            }

            builderBuilder.Sort((a, b) =>
            {
                var l = a.r.Start.Line.CompareTo(b.r.Start.Line);
                return (l != 0) ? l : a.r.Start.Character.CompareTo(b.r.Start.Character);
            });
            foreach (var p in builderBuilder)
            {
                builder.Push(p.r, p.t, 0);
            }
        }
    }
}
