using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Thousand.LSP
{
    class SemanticTokensHandler : SemanticTokensHandlerBase
    {
        private readonly SemanticService semanticService;

        public SemanticTokensHandler(SemanticService semanticService)
        {
            this.semanticService = semanticService;
        }

        protected override SemanticTokensRegistrationOptions CreateRegistrationOptions(SemanticTokensCapability capability, ClientCapabilities clientCapabilities) => new()
        {
            DocumentSelector = DocumentSelector.ForLanguage("thousand"),
            Full = new SemanticTokensCapabilityRequestFull() { Delta = true },
            Range = true,
            Legend = new ()
            {
                TokenTypes = new[] { SemanticTokenType.Class }
            }
        };

        protected override Task<SemanticTokensDocument> GetSemanticTokensDocument(ITextDocumentIdentifierParams @params, CancellationToken cancellationToken)
        {
            return Task.FromResult(new SemanticTokensDocument(RegistrationOptions.Legend));
        }

        protected override async Task Tokenize(SemanticTokensBuilder builder, ITextDocumentIdentifierParams identifier, CancellationToken cancellationToken)
        {
            var document = await semanticService.GetParseAsync(identifier.TextDocument.Uri);
            if (document.Syntax != null)
            {
                foreach (var topLevelClass in document.Syntax.Declarations.Where(d => d.Value.IsT2).Select(d => d.Value.AsT2))
                {
                    HighlightClass(builder, topLevelClass);
                }

                foreach (var topLevelObject in document.Syntax.Declarations.Where(d => d.Value.IsT3).Select(d => d.Value.AsT3))
                {
                    HighlightObject(builder, topLevelObject);
                }

                foreach (var topLevelLine in document.Syntax.Declarations.Where(d => d.Value.IsT4).Select(d => d.Value.AsT4))
                {
                    HighlightLine(builder, topLevelLine);
                }
            }
        }

        private static void HighlightClass(SemanticTokensBuilder builder, AST.TolerantClass klass)
        {
            builder.Push(klass.Name.Span.AsRange(), 0, 0);

            foreach (var call in klass.BaseClasses)
            {
                builder.Push(call.Span().AsRange(), 0, 0);
            }

            foreach (var nestedClass in klass.Declarations.Where(d => d.Value.IsT2).Select(d => d.Value.AsT2))
            {
                HighlightClass(builder, nestedClass);
            }

            foreach (var nestedObject in klass.Declarations.Where(d => d.Value.IsT3).Select(d => d.Value.AsT3))
            {
                HighlightObject(builder, nestedObject);
            }

            foreach (var nestedLine in klass.Declarations.Where(d => d.Value.IsT4).Select(d => d.Value.AsT4))
            {
                HighlightLine(builder, nestedLine);
            }
        }

        private static void HighlightObject(SemanticTokensBuilder builder, AST.TolerantObject objekt)
        {
            foreach (var call in objekt.Classes)
            {
                builder.Push(call.Span().AsRange(), 0, 0);
            }

            foreach (var nestedClass in objekt.Declarations.Where(d => d.Value.IsT2).Select(d => d.Value.AsT2))
            {
                HighlightClass(builder, nestedClass);
            }

            foreach (var nestedObject in objekt.Declarations.Where(d => d.Value.IsT3).Select(d => d.Value.AsT3))
            {
                HighlightObject(builder, nestedObject);
            }

            foreach (var nestedLine in objekt.Declarations.Where(d => d.Value.IsT4).Select(d => d.Value.AsT4))
            {
                HighlightLine(builder, nestedLine);
            }
        }

        private static void HighlightLine(SemanticTokensBuilder builder, AST.UntypedLine line)
        {
            foreach (var call in line.Classes)
            {
                builder.Push(call.Span().AsRange(), 0, 0);
            }
        }
    }
}
