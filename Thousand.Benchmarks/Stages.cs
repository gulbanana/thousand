using BenchmarkDotNet.Attributes;
using Superpower;
using Superpower.Model;
using System;
using System.IO;
using Thousand.API;
using Thousand.Compose;
using Thousand.Evaluate;
using Thousand.Parse;
using Thousand.Render;

namespace Thousand.Benchmarks
{
    public class Stages
    {
        [Params("connectors.1000", "tetris.1000", "underground.1000")]
        public string Input { get; set; } = default!;

        private readonly Tokenizer<TokenKind> tokenizer;
        private readonly SVGRenderer renderer;
        private string source;
        private Metadata api;
        private AST.TypedDocument stdlibAST;
        private AST.UntypedDocument preprocessedAST;
        private AST.TypedDocument typedAST;
        private IR.Root rules;
        private Layout.Diagram diagram;
        private TokenList<TokenKind> tokens;

        public Stages()
        {
            tokenizer = Tokenizer.Build();
            renderer = new SVGRenderer();
        }

        [GlobalSetup]
        public void Setup()
        {
            source = File.ReadAllText(Input);

            api = new Metadata();
            var state = new GenerationState();

            Preprocessor.TryPreprocess(state, DiagramGenerator.ReadStdlib(), out var stdlibMacros);
            Typechecker.TryTypecheck(api, state, stdlibMacros, false, out stdlibAST);

            tokens = tokenizer.Tokenize(source);
            Preprocessor.TryPreprocess(state, source, out preprocessedAST);
            Typechecker.TryTypecheck(api, state, preprocessedAST, false, out typedAST);
            Evaluator.TryEvaluate(new[] { stdlibAST, typedAST }, state, out rules);
            Composer.TryCompose(rules, state, out diagram);

            if (state.HasErrors())
            {
                throw new Exception("source invalid, cannot benchmark");
            }
        }

        [Benchmark]
        public TokenList<TokenKind> Tokenize()
        {
            return tokenizer.Tokenize(source);
        }

        [Benchmark]
        public bool Preprocess()
        {
            return Preprocessor.TryPreprocess(new GenerationState(), tokens, out _);
        }

        [Benchmark]
        public bool Typecheck()
        {
            return Typechecker.TryTypecheck(api, new GenerationState(), preprocessedAST, false, out _);
        }

        [Benchmark]
        public bool Evaluate()
        {
            return Evaluator.TryEvaluate(new[] { stdlibAST, typedAST }, new GenerationState(), out _);
        }

        [Benchmark]
        public bool Compose()
        {
            return Composer.TryCompose(rules, new GenerationState(), out _);
        }

        [Benchmark]
        public string Render()
        {
            return renderer.Render(diagram).ToString();
        }
    }
}
