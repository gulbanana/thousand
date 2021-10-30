using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol;
using Superpower;
using Superpower.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Thousand.Parse;

namespace Thousand.LSP.Analyse
{
    public class AnalysisService
    {
        private readonly Dictionary<DocumentUri, (Task<Analysis> task, CancellationTokenSource cts)> analyses = new();
        private readonly ILogger<AnalysisService> logger;
        private readonly BufferService documentService;
        private readonly IDiagnosticService diagnosticService;
        private readonly IGenerationService generationService;

        private readonly Tokenizer<TokenKind> tokenizer;
        private readonly API.Metadata api;

        private readonly ParsedDocument? stdlib;
        private readonly AST.TypedDocument? typedStdlib;

        public AnalysisService(ILogger<AnalysisService> logger, API.Metadata api, BufferService documentService, IDiagnosticService diagnosticService, IGenerationService generationService)
        {
            this.logger = logger;
            this.documentService = documentService;
            this.diagnosticService = diagnosticService;
            this.generationService = generationService;

            this.api = api;
            this.tokenizer = Tokenizer.Build();

            var stdlibState = new GenerationState();
            var stdlibSource = DiagramGenerator.ReadStdlib();
            var stdlibEnd = Shared.GetEnd(stdlibSource);
            var stdlibTokens = tokenizer.TryTokenize(stdlibSource);
            if (!stdlibTokens.HasValue || !Preprocessor.TryPreprocess(stdlibState, stdlibEnd, stdlibTokens.Value, out var stdlibSyntax) 
                                       || !Typechecker.TryTypecheck(api, stdlibState, stdlibSyntax, stdlibEnd, allowErrors: false, out typedStdlib))
            {
                logger.LogError($"Failed to parse stdlib: {stdlibState.JoinErrors()}");
            }
            else
            {
                stdlib = new ParsedDocument(new DocumentUri("thousand", null, "stdlib.1000", null, null), stdlibEnd, stdlibSyntax);
            }
        }

        public void Reparse(DocumentUri key, ServerOptions options)
        {
            var cts = new CancellationTokenSource();
            lock (analyses)
            {
                if (!analyses.TryGetValue(key, out var pending))
                {
                    analyses[key] = (Task.Run(() => Analyse(options, key, cts.Token)), cts);
                }
                else
                {
                    if (pending.task.IsCompleted)
                    {
                        analyses[key] = (Task.Run(() => Analyse(options, key, cts.Token)), cts);
                    }
                    else
                    {
                        analyses[key] = (Task.Run(async () =>
                        {
                            await Task.WhenAny(pending.task, Task.Delay(100)); // XXX the problem with this is that it will leave an infinitely looping t looping 
                            if (!pending.task.IsCompleted)
                            {
                                logger.LogWarning($"Hanging analysis leaked for {key}");
                            }
                            return Analyse(options, key, cts.Token);
                        }), cts);
                    }
                }
            }
        }

        public Task<Analysis> GetAnalysisAsync(DocumentUri key)
        {
            return analyses[key].task;
        }

        public Analysis Analyse(ServerOptions options, DocumentUri key, CancellationToken ct)
        {
            var stopwatch = Stopwatch.StartNew();

            var source = documentService.GetText(key); // XXX is this a race condition?
            var state = new GenerationState();
            var doc = new Analysis();

            try
            {
                AnalyseSyntax(options, state, doc, key, source, ct);
            }
            catch (Exception e)
            {
                logger.LogError(e, e.Message);
            }

            stopwatch.Stop();
            logger.LogInformation("Analysed {Uri} in {ElapsedMilliseconds}ms", key, stopwatch.ElapsedMilliseconds);

            if (ct.IsCancellationRequested) return doc;

            diagnosticService.Update(key, state, source);

            if (doc.Diagram != null)
            {
                generationService.Update(key, doc.Diagram);
            }

            try
            {
                AnalyseSemantics(doc, ct);
            }
            catch (Exception e)
            {
                logger.LogError(e, e.Message);
            }            

            return doc;
        }

        private void AnalyseSyntax(ServerOptions options, GenerationState state, Analysis analysis, DocumentUri uri, string source, CancellationToken ct)
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

            analysis.Tokens = untypedTokens.Value;
            if (ct.IsCancellationRequested) return;

            // parse the document into an error-tolerant structure which preserves errors as well as valid content
            var end = Shared.GetEnd(source);
            var untypedAST = Untyped.Document(untypedTokens.Value);
            if (!untypedAST.HasValue || !untypedAST.Remainder.IsAtEnd)
            {
                state.AddError(untypedTokens.Value, end, untypedAST);
                return;
            }

            analysis.Main = new ParsedDocument(uri, end, untypedAST.Value);
            if (ct.IsCancellationRequested) return;

            // apply the next stages of parsing, jumping into the pipeline mid-stream
            if (!Preprocessor.TryPreprocess(state, analysis.Main.EndSpan, untypedTokens.Value, untypedAST.Value, out var syntax))
            {
                return;
            }

            if (!Typechecker.TryTypecheck(api, state, syntax, analysis.Main.EndSpan, allowErrors: true, out var typedAST))
            {
                return;
            }

            analysis.ValidSyntax = typedAST;
            if (ct.IsCancellationRequested) return;

            // generate as much of the diagram as possible
            if (options.NoStandardLibrary)
            {
                if (!Evaluate.Evaluator.TryEvaluate(new[] { typedAST }, state, out var rules))
                {
                    return;
                }

                analysis.Root = rules;
                if (ct.IsCancellationRequested) return;

                if (!Compose.Composer.TryCompose(rules, state, out var diagram))
                {
                    return;
                }

                analysis.Diagram = diagram;
            }
            else if (typedStdlib != null)
            {
                analysis.Stdlib = stdlib;

                if (!Evaluate.Evaluator.TryEvaluate(new[] { typedStdlib, typedAST }, state, out var rules))
                {
                    return;
                }

                analysis.Root = rules;
                if (ct.IsCancellationRequested) return;

                if (!Compose.Composer.TryCompose(rules, state, out var diagram))
                {
                    return;
                }

                analysis.Diagram = diagram;
            }
        }

        private void AnalyseSemantics(Analysis analysis, CancellationToken ct)
        {
            var root = new UntypedScope("root");
            
            if (analysis.Stdlib != null)
            {
                Walker.WalkDocument(analysis, analysis.Stdlib, root);
            }

            if (ct.IsCancellationRequested) return;

            if (analysis.Main != null)
            {
                Walker.WalkDocument(analysis, analysis.Main, root);
            }
        }
    }
}
