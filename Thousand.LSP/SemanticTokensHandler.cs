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
        private readonly AnalysisService semanticService;

        public SemanticTokensHandler(AnalysisService semanticService)
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

            foreach (var (cref, cdef) in document.ClassReferences)
            {
                builder.Push(cref.AsRange(), 0, 0);
            }

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
            foreach (var attribute in klass.Attributes)
            {
                foreach (var token in attribute.Value.Sequence())
                {
                    if (token.Kind == Parse.TokenKind.Identifier)
                    {
                        builder.Push(token.Span.AsRange(), 1, 0);
                    }
                }
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
            foreach (var attribute in objekt.Attributes)
            {
                foreach (var token in attribute.Sequence().SkipWhile(t => t.Kind != Parse.TokenKind.EqualsSign).Skip(1))
                {
                    if (token.Kind == Parse.TokenKind.Identifier)
                    {
                        builder.Push(token.Span.AsRange(), 1, 0);
                    }
                }
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

        private static void HighlightLine(SemanticTokensBuilder builder, AST.TolerantLine line)
        {
            foreach (var attribute in line.Attributes)
            {
                foreach (var token in attribute.Sequence().SkipWhile(t => t.Kind != Parse.TokenKind.EqualsSign).Skip(1))
                {
                    if (token.Kind == Parse.TokenKind.Identifier)
                    {
                        builder.Push(token.Span.AsRange(), 1, 0);
                    }
                }
            }
        }
    }
}
