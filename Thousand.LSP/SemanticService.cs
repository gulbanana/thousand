using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol;
using Superpower;
using Superpower.Model;
using System;
using System.Collections.Generic;
using System.Linq;
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
            this.tokenizer = Tokenizer.Build();

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
                    parses[key] = Task.Run(() => Analyse(key));
                }
                else
                {
                    parses[key] = Task.Run(async () =>
                    {
                        await t;
                        return Analyse(key);
                    });
                }
            }
        }

        public Task<SemanticDocument> GetParseAsync(DocumentUri key)
        {
            return parses[key];
        }

        private SemanticDocument Analyse(DocumentUri key)
        {
            var source = documentService.GetText(key); // XXX is this a race condition?
            var state = new GenerationState();
            var doc = new SemanticDocument(key); 
            
            AnalysePartial(state, doc, source);

            diagnosticService.Update(key, state);

            return doc;
        }

        private void AnalysePartial(GenerationState state, SemanticDocument doc, string source)
        {
            // tokenize the whole document. unlike parsing, this is not line-by-line, so a single
            // ! will result in untypedTokens.Location extending to the end of the document
            // assume the bad "token" is \W and, if possible, parse everything up to it

            // XXX we could improve this further by *continuing* tokenization from the next lineseparator and stitching the parts
            var untypedTokens = tokenizer.TryTokenize(source);
            if (!untypedTokens.HasValue)
            {
                var badCount = untypedTokens.Location.ToStringValue().TakeWhile(c => !char.IsWhiteSpace(c)).Count();
                var badSpan = new TextSpan(source, untypedTokens.ErrorPosition, badCount);
                state.AddError(badSpan, ErrorKind.Syntax, untypedTokens.FormatErrorMessageFragment());
                
                if (untypedTokens.ErrorPosition.Absolute == 0)
                {
                    return;
                }
                else
                {
                    untypedTokens = tokenizer.TryTokenize(source[..untypedTokens.ErrorPosition.Absolute]);
                }
            }

            doc.Tokens = untypedTokens.Value;

            // parse the document with a special overlay allowing entire declarations to fail and preserving the valid content
            var tolerantAST = Tolerant.Document(untypedTokens.Value);
            if (!tolerantAST.HasValue || !tolerantAST.Remainder.IsAtEnd)
            {
                if (!tolerantAST.HasValue)
                {
                    var badToken = tolerantAST.Location.IsAtEnd ? untypedTokens.Value.Last() : tolerantAST.Location.First();
                    state.AddError(badToken.Span, ErrorKind.Syntax, tolerantAST.FormatErrorMessageFragment());
                }
                return;
            }

            doc.Syntax = tolerantAST.Value;

            // splice out the bad declarations, recording them as errors as we go
            var splices = SpliceDocument(state, tolerantAST.Value).ToList();

            var tokensWithoutErrors = untypedTokens.Value;
            foreach (var splice in splices.OrderByDescending(s => s.Location.Start.Value))
            {
                tokensWithoutErrors = splice.Apply(tokensWithoutErrors);
            }

            // apply the standard multipass parser, supplying a synthetic macro-level token stream with the errors excised
            // we can't simply convert the tolerant AST to an untyped AST, because its macro positions would be wrong
            if (!Parser.TryParse(tokensWithoutErrors, state, out var typedAST))
            {
                return;
            }

            doc.ValidSyntax = typedAST;

            // generate as much of the diagram as possible
            if (stdlib != null)
            { 
                if (!Evaluate.Evaluator.TryEvaluate(new[] { stdlib, typedAST }, state, out var rules))
                {
                    return;
                }

                doc.Rules = rules;

                if (!Compose.Composer.TryCompose(rules, state, out var diagram))
                {
                    return;
                }

                doc.Diagram = diagram;
            }
        }

        private IEnumerable<Splice> SpliceDocument(GenerationState state, AST.TolerantDocument ast)
        {
            foreach (var declaration in ast.Declarations)
            {
                if (declaration.Value.IsT0)
                {
                    var subTokens = new TokenList<TokenKind>(declaration.Sequence().ToArray());
                    var error = Untyped.DocumentContent(subTokens);
                    var badToken = error.Location.IsAtEnd ? subTokens.Last() : error.Location.First();
                    state.AddError(badToken.Span, ErrorKind.Syntax, error.FormatErrorMessageFragment());

                    yield return new Splice(declaration.Range(), Array.Empty<Token<TokenKind>>());
                }
                else if (declaration.Value.IsT3)
                {
                    foreach (var splice in SpliceObject(state, declaration.Value.AsT3))
                    {
                        yield return splice;
                    }
                }
            }
        }

        private IEnumerable<Splice> SpliceObject(GenerationState state, AST.TolerantObject ast)
        {
            foreach (var declaration in ast.Declarations)
            {
                if (declaration.Value.IsT0)
                {
                    var subTokens = new TokenList<TokenKind>(declaration.Sequence().ToArray());
                    var error = Untyped.DocumentContent(subTokens);
                    var badToken = error.Location.IsAtEnd ? subTokens.Last() : error.Location.First();
                    state.AddError(badToken.Span, ErrorKind.Syntax, error.FormatErrorMessageFragment());

                    yield return new Splice(declaration.Range(), Array.Empty<Token<TokenKind>>());
                }
            }
        }
    }
}
