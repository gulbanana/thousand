using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Superpower;
using Superpower.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Thousand.Parse;

namespace Thousand.LSP.Analyse
{
    public class AnalysisService
    {
        private readonly Dictionary<DocumentUri, Task<Analysis>> analyses = new();
        private readonly ILogger<AnalysisService> logger;
        private readonly BufferService documentService;
        private readonly IDiagnosticService diagnosticService;
        private readonly IGenerationService generationService;

        private readonly Tokenizer<TokenKind> tokenizer;
        private readonly Parse.Attributes.API api;

        private readonly ParsedDocument? stdlib;
        private readonly AST.TypedDocument? typedStdlib;

        public AnalysisService(ILogger<AnalysisService> logger, BufferService documentService, IDiagnosticService diagnosticService, IGenerationService generationService)
        {
            this.logger = logger;
            this.documentService = documentService;
            this.diagnosticService = diagnosticService;
            this.generationService = generationService;

            this.api = new Parse.Attributes.API();
            this.tokenizer = Tokenizer.Build();

            var stdlibState = new GenerationState();
            var stdlibSource = DiagramGenerator.ReadStdlib();
            if (!Preprocessor.TryPreprocess(stdlibState, DiagramGenerator.ReadStdlib(), out var stdlibSyntax) || !Typechecker.TryTypecheck(api, stdlibState, stdlibSyntax, allowErrors: false, out typedStdlib))
            {
                logger.LogError($"Failed to parse stdlib: {stdlibState.JoinErrors()}");
            }
            else
            {
                stdlib = new ParsedDocument(new DocumentUri("thousand", null, "stdlib.1000", null, null), stdlibSource, stdlibSyntax);
            }
        }

        public void Reparse(DocumentUri key, ServerOptions options)
        {
            lock (analyses)
            {
                if (!analyses.TryGetValue(key, out var t))
                {
                    analyses[key] = Task.Run(() => Analyse(options, key));
                }
                else
                {
                    if (t.IsCompleted)
                    {
                        analyses[key] = Task.Run(() => Analyse(options, key));
                    }
                    else
                    {
                        analyses[key] = Task.Run(async () =>
                        {
                            await t;
                            return Analyse(options, key);
                        });
                    }
                }
            }
        }

        public Task<Analysis> GetAnalysisAsync(DocumentUri key)
        {
            return analyses[key];
        }

        public Analysis Analyse(ServerOptions options, DocumentUri key)
        {
            var stopwatch = Stopwatch.StartNew();

            var source = documentService.GetText(key); // XXX is this a race condition?
            var state = new GenerationState();
            var doc = new Analysis();

            try
            {
                AnalyseSyntax(options, state, doc, key, source);
            }
            catch (Exception e)
            {
                logger.LogError(e, e.Message);
            }

            stopwatch.Stop();
            logger.LogInformation("Analysed {Uri} in {ElapsedMilliseconds}ms", key, stopwatch.ElapsedMilliseconds);

            diagnosticService.Update(key, state);

            if (doc.Diagram != null)
            {
                generationService.Update(key, doc.Diagram);
            }

            try
            {
                AnalyseSemantics(doc);
            }
            catch (Exception e)
            {
                logger.LogError(e, e.Message);
            }            

            return doc;
        }

        private void AnalyseSyntax(ServerOptions options, GenerationState state, Analysis analysis, DocumentUri uri, string source)
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

            // parse the document into an error-tolerant structure which preserves errors as well as valid content
            var untypedAST = Untyped.Document(untypedTokens.Value);
            if (!untypedAST.HasValue || !untypedAST.Remainder.IsAtEnd)
            {
                state.AddError(untypedTokens.Value, untypedAST);
                return;
            }

            analysis.Main = new ParsedDocument(uri, source, untypedAST.Value);

            // apply the next stages of parsing, jumping into the pipeline mid-stream
            if (!Preprocessor.TryPreprocess(state, untypedTokens.Value, untypedAST.Value, out var syntax))
            {
                return;
            }

            if (!Typechecker.TryTypecheck(api, state, syntax, allowErrors: true, out var typedAST))
            {
                return;
            }

            analysis.ValidSyntax = typedAST;

            // generate as much of the diagram as possible
            if (options.NoStandardLibrary)
            {
                if (!Evaluate.Evaluator.TryEvaluate(new[] { typedAST }, state, out var rules))
                {
                    return;
                }

                analysis.Rules = rules;

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

                analysis.Rules = rules;

                if (!Compose.Composer.TryCompose(rules, state, out var diagram))
                {
                    return;
                }

                analysis.Diagram = diagram;
            }
        }

        private void AnalyseSemantics(Analysis analysis)
        {
            var root = new AnalysisScope();

            if (analysis.Stdlib != null)
            {
                WalkDocument(analysis, analysis.Stdlib, root);
            }

            if (analysis.Main != null)
            {
                WalkDocument(analysis, analysis.Main, root);
            }
        }

        private void WalkDocument(Analysis analysis, ParsedDocument doc, AnalysisScope root)
        {
            foreach (var dec in doc.Syntax.Declarations)
            {
                dec.Value.Switch(invalid => { }, asAttribute =>
                {
                    analysis.Attributes.Add(asAttribute);
                }, asClass =>
                {
                    WalkClass(analysis, doc.Uri, root, asClass);
                    root.Pop(asClass);
                }, asObject =>
                {
                    WalkObject(analysis, doc.Uri, root, asObject);
                    root.Pop(asObject);
                }, asLine =>
                {
                    WalkLine(analysis, doc.Uri, root, asLine);
                });
            }
        }

        private void WalkClass(Analysis analysis, DocumentUri uri, AnalysisScope scope, AST.UntypedClass ast)
        {
            analysis.ClassDefinitions[ast] = new Location { Uri = uri, Range = ast.Name.Span.AsRange()};

            analysis.ClassReferences.Add(new(uri, ast.Name, ast));

            var classes = new List<AST.UntypedClass>();
            foreach (var callMacro in ast.BaseClasses)
            {
                var klass = scope.FindClass(callMacro.Value.Name);
                analysis.ClassReferences.Add(new(uri, callMacro, klass));
                if (klass is not null)
                {
                    classes.Add(klass);
                }
            }
            analysis.ClassClasses[ast] = classes;

            // XXX if we do proper imports, we need to store what document attributes came from
            if (uri.Scheme != "thousand")
            {
                foreach (var attribute in ast.Attributes)
                {
                    analysis.Attributes.Add(attribute);
                }
            }

            var contents = scope.Push();

            foreach (var dec in ast.Declarations)
            {
                dec.Value.Switch(invalid => { }, asAttribute =>
                {
                    analysis.Attributes.Add(asAttribute);
                }, asClass =>
                {
                    WalkClass(analysis, uri, contents, asClass);
                    contents.Pop(asClass);
                }, asObject =>
                {
                    WalkObject(analysis, uri, contents, asObject);
                    contents.Pop(asObject);
                }, asLine =>
                {
                    WalkLine(analysis, uri, contents, asLine);
                });
            }
        }

        private void WalkObject(Analysis analysis, DocumentUri uri, AnalysisScope scope, AST.UntypedObject ast)
        {
            // XXX this duplicates logic from Evaluator
            var name = ast.Name;
            if (name == null)
            {
                var classExpression = string.Join('.', ast.Classes.Select(c => c.Value.Name.Text));
                var firstClass = ast.Classes.First().Value.Name.Span;
                name = new Identifier(classExpression) { Span = new(firstClass.Source!, firstClass.Position, firstClass.Length) }; // XXX use all sources
            }

            analysis.ObjectDefinitions[ast] = new Location { Uri = uri, Range = name.Span.AsRange() };

            if (ast.Name != null)
            {
                analysis.ObjectReferences.Add(new(uri, ast.Name, ast));
            }

            var classes = new List<AST.UntypedClass>();
            foreach (var callMacro in ast.Classes)
            {
                var klass = scope.FindClass(callMacro.Value.Name);
                analysis.ClassReferences.Add(new(uri, callMacro, klass));
                if (klass is not null)
                {
                    classes.Add(klass);
                }
            }
            analysis.ObjectClasses[ast] = classes;

            foreach (var attribute in ast.Attributes)
            {
                analysis.Attributes.Add(attribute);
            }

            var contents = scope.Push();

            foreach (var dec in ast.Declarations)
            {
                dec.Value.Switch(invalid => { }, asAttribute =>
                {
                    analysis.Attributes.Add(asAttribute);
                }, asClass =>
                {
                    WalkClass(analysis, uri, contents, asClass);
                    contents.Pop(asClass);
                }, asObject =>
                {
                    WalkObject(analysis, uri, contents, asObject);
                    contents.Pop(asObject);
                }, asLine =>
                {
                    WalkLine(analysis, uri, contents, asLine);
                });
            }
        }

        private void WalkLine(Analysis analysis, DocumentUri uri, AnalysisScope  scope, AST.UntypedLine ast)
        {
            foreach (var attribute in ast.Attributes)
            {
                analysis.Attributes.Add(attribute);
            }

            foreach (var segment in ast.Segments)
            {
                if (scope.FindObject(segment.Target) is AST.UntypedObject objekt)
                {
                    analysis.ObjectReferences.Add(new(uri, segment.Target, objekt));
                }               
            }

            foreach (var callMacro in ast.Classes)
            {
                analysis.ClassReferences.Add(new(uri, callMacro, scope.FindClass(callMacro.Value.Name)));
            }
        }
    }
}
