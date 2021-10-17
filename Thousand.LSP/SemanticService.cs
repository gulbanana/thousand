using OmniSharp.Extensions.LanguageServer.Protocol;
using Superpower;
using System.Collections.Generic;
using System.Threading.Tasks;
using Thousand.Parse;

namespace Thousand.LSP
{
    public class SemanticService
    {
        private readonly Dictionary<DocumentUri, Task<SemanticDocument>> parses = new();
        private readonly BufferService documentService;
        private readonly Tokenizer<TokenKind> tokenizer;

        public SemanticService(BufferService documentService)
        {
            this.documentService = documentService;
            this.tokenizer = Tokenizer.Build(true);
        }

        public void Reparse(DocumentUri key)
        {
            if (!parses.TryGetValue(key, out var t))
            {
                parses[key] = Task.Run(() => Parse(documentService.GetText(key)));
            }
            else
            {
                parses[key] = Task.Run(async () =>
                {
                    await t;
                    return Parse(documentService.GetText(key));
                });
            }
        }

        public Task<SemanticDocument> GetParseAsync(DocumentUri key)
        {
            return parses[key];
        }

        private SemanticDocument Parse(string source)
        {
            var doc = new SemanticDocument();

            var untypedTokens = tokenizer.TryTokenize(source);
            if (!untypedTokens.HasValue) return doc;
            doc.Tokens = untypedTokens.Value;

            var untypedAST = Tolerant.Document(untypedTokens.Value);
            if (!untypedAST.HasValue || !untypedAST.Remainder.IsAtEnd) return doc;
            doc.Syntax = untypedAST.Value;

            return doc;
        }
    }
}
