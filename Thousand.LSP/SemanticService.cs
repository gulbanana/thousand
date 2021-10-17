using Microsoft.Extensions.Logging;
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
        private readonly ILogger<SemanticService> logger;
        private readonly BufferService documentService;
        private readonly IDiagnosticService diagnosticService;
        private readonly Tokenizer<TokenKind> tokenizer;
        private readonly AST.TypedDocument? stdlib;

        public SemanticService(ILogger<SemanticService> logger, BufferService documentService, IDiagnosticService diagnosticService)
        {
            this.logger = logger;
            this.documentService = documentService;
            this.diagnosticService = diagnosticService;
            this.tokenizer = Tokenizer.Build(true);

            var stdlibState = new GenerationState();
            if (!Parser.TryParse(DiagramGenerator.ReadStdlib(), stdlibState, out stdlib))
            {
                logger.LogError($"Failed to parse stdlib: {stdlibState.JoinErrors()}");
            }
        }

        public void Reparse(DocumentUri key)
        {
            lock (parses)
            {
                if (!parses.TryGetValue(key, out var t))
                {
                    parses[key] = Task.Run(() => Parse(key));
                }
                else
                {
                    parses[key] = Task.Run(async () =>
                    {
                        await t;
                        return Parse(key);
                    });
                }
            }
        }

        public Task<SemanticDocument> GetParseAsync(DocumentUri key)
        {
            return parses[key];
        }

        private SemanticDocument Parse(DocumentUri key)
        {
            var source = documentService.GetText(key); // XXX is this a race condition?
            var doc = new SemanticDocument(key);

            var untypedTokens = tokenizer.TryTokenize(source);
            if (!untypedTokens.HasValue) return doc;
            doc.Tokens = untypedTokens.Value;

            var tolerantAST = Tolerant.Document(untypedTokens.Value);
            if (tolerantAST.HasValue && tolerantAST.Remainder.IsAtEnd)
            {
                doc.Syntax = tolerantAST.Value;
            }

            // as long as it tokenizes, we can try the whole process and get our standard errors
            if (stdlib != null)
            {
                var state = new GenerationState();
                if (Parser.TryParse(source, state, out var typedAST) && Evaluate.Evaluator.TryEvaluate(new[] { stdlib, typedAST }, state, out var rules) && Compose.Composer.TryCompose(rules, state, out var diagram))
                {
                    doc.Diagram = diagram;
                }
                diagnosticService.Update(key, state);
            }

            return doc;
        }
    }
}
