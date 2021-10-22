using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol;
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
        private readonly Parse.Attributes.Metadata metadata;

        private readonly AST.TypedDocument? stdlib;

        public AnalysisService(ILogger<AnalysisService> logger, BufferService documentService, IDiagnosticService diagnosticService, IGenerationService generationService)
        {
            this.logger = logger;
            this.documentService = documentService;
            this.diagnosticService = diagnosticService;
            this.generationService = generationService;

            this.metadata = new Parse.Attributes.Metadata();
            this.tokenizer = Tokenizer.Build();

            var stdlibState = new GenerationState();
            if (!Preprocessor.TryPreprocess(stdlibState, DiagramGenerator.ReadStdlib(), out var syntax) || !Typechecker.TryTypecheck(metadata, stdlibState, syntax, allowErrors: false, out stdlib))
            {
                logger.LogError($"Failed to parse stdlib: {stdlibState.JoinErrors()}");
            }
        }

        public void Reparse(DocumentUri key)
        {
            lock (analyses)
            {
                if (!analyses.TryGetValue(key, out var t))
                {
                    analyses[key] = Task.Run(() => Analyse(key));
                }
                else
                {
                    if (t.IsCompleted)
                    {
                        analyses[key] = Task.Run(() => Analyse(key));
                    }
                    else
                    {
                        analyses[key] = Task.Run(async () =>
                        {
                            await t;
                            return Analyse(key);
                        });
                    }
                }
            }
        }

        public Task<Analysis> GetAnalysisAsync(DocumentUri key)
        {
            return analyses[key];
        }

        public Analysis Analyse(DocumentUri key)
        {
            var stopwatch = Stopwatch.StartNew();

            var source = documentService.GetText(key); // XXX is this a race condition?
            var state = new GenerationState();
            var doc = new Analysis(key);

            try
            {
                AnalyseSyntax(doc, state, source);
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

        private void AnalyseSyntax(Analysis doc, GenerationState state, string source)
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

            // parse the document into an error-tolerant structure which preserves errors as well as valid content
            var untypedAST = Untyped.Document(untypedTokens.Value);
            if (!untypedAST.HasValue || !untypedAST.Remainder.IsAtEnd)
            {
                state.AddError(untypedTokens.Value, untypedAST);
                return;
            }

            doc.Syntax = untypedAST.Value;

            // apply the next stages of parsing, jumping into the pipeline mid-stream
            if (!Preprocessor.TryPreprocess(state, untypedTokens.Value, untypedAST.Value, out var syntax))
            {
                return;
            }

            if (!Typechecker.TryTypecheck(metadata, state, syntax, allowErrors: true, out var typedAST))
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

        private void AnalyseSemantics(Analysis doc)
        {
            if (doc.Syntax != null)
            {
                WalkDocument(doc, doc.Syntax);
            }
        }

        private void WalkDocument(Analysis doc, AST.UntypedDocument ast)
        {
            var root = new AnalysisScope();

            foreach (var dec in ast.Declarations)
            {
                dec.Value.Switch(invalid => { }, asAttribute =>
                {
                    doc.Attributes.Add(asAttribute);
                }, asClass =>
                {
                    WalkClass(doc, root, asClass);
                    root.Pop(asClass);
                }, asObject =>
                {
                    WalkObject(doc, root, asObject);
                    root.Pop(asObject);
                }, asLine =>
                {
                    WalkLine(doc, root, asLine);
                });
            }
        }

        private void WalkClass(Analysis doc, AnalysisScope scope, AST.UntypedClass ast)
        {
            if (ast.Name != null)
            {
                doc.ClassReferences.Add(new(ast.Name, ast));
            }

            foreach (var callMacro in ast.BaseClasses)
            {
                doc.ClassReferences.Add(new(callMacro, scope.FindClass(callMacro.Value.Name)));
            }

            foreach (var attribute in ast.Attributes)
            {
                doc.Attributes.Add(attribute);
            }

            var contents = scope.Push();

            foreach (var dec in ast.Declarations)
            {
                dec.Value.Switch(invalid => { }, asAttribute =>
                {
                    doc.Attributes.Add(asAttribute);
                }, asClass =>
                {
                    WalkClass(doc, contents, asClass);
                    contents.Pop(asClass);
                }, asObject =>
                {
                    WalkObject(doc, contents, asObject);
                    contents.Pop(asObject);
                }, asLine =>
                {
                    WalkLine(doc, contents, asLine);
                });
            }
        }

        private void WalkObject(Analysis doc, AnalysisScope scope, AST.UntypedObject ast)
        {
            if (ast.Name != null)
            {
                doc.ObjectReferences.Add(new(ast.Name, ast));
            }

            foreach (var callMacro in ast.Classes)
            {
                doc.ClassReferences.Add(new(callMacro, scope.FindClass(callMacro.Value.Name)));
            }

            foreach (var attribute in ast.Attributes)
            {
                doc.Attributes.Add(attribute);
            }

            var contents = scope.Push();

            foreach (var dec in ast.Declarations)
            {
                dec.Value.Switch(invalid => { }, asAttribute =>
                {
                    doc.Attributes.Add(asAttribute);
                }, asClass =>
                {
                    WalkClass(doc, contents, asClass);
                    contents.Pop(asClass);
                }, asObject =>
                {
                    WalkObject(doc, contents, asObject);
                    contents.Pop(asObject);
                }, asLine =>
                {
                    WalkLine(doc, contents, asLine);
                });
            }
        }

        private void WalkLine(Analysis doc, AnalysisScope  scope, AST.UntypedLine ast)
        {
            foreach (var attribute in ast.Attributes)
            {
                doc.Attributes.Add(attribute);
            }

            foreach (var segment in ast.Segments)
            {
                if (scope.FindObject(segment.Target) is AST.UntypedObject objekt)
                {
                    doc.ObjectReferences.Add(new(segment.Target, objekt));
                }               
            }

            foreach (var callMacro in ast.Classes)
            {
                doc.ClassReferences.Add(new(callMacro, scope.FindClass(callMacro.Value.Name)));
            }
        }
    }
}
