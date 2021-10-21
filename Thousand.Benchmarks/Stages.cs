using BenchmarkDotNet.Attributes;
using System;
using System.IO;
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

        private readonly SkiaRenderer renderer;
        private string source;
        private AST.TypedDocument stdlib;
        private AST.TypedDocument ast;
        private IR.Root rules;
        private Layout.Diagram diagram;
        
        public Stages()
        {
            renderer = new SkiaRenderer();
        }

        [GlobalSetup]
        public void Setup()
        {
            source = File.ReadAllText(Input);

            var state = new GenerationState();
            Parser.TryParse(DiagramGenerator.ReadStdlib(), state, out stdlib);
            Parser.TryParse(source, state, out ast);
            Evaluator.TryEvaluate(new[] { stdlib, ast }, state, out rules);
            Composer.TryCompose(rules, state, out diagram);

            if (state.HasErrors())
            {
                throw new Exception("source invalid, cannot benchmark");
            }
        }

        [Benchmark]
        public bool Parse()
        {
            return Parser.TryParse(source, new GenerationState(), out _);
        }

        [Benchmark]
        public bool Evaluate()
        {
            return Evaluator.TryEvaluate(new[] { stdlib, ast }, new GenerationState(), out _);
        }

        [Benchmark]
        public bool Compose()
        {
            return Composer.TryCompose(rules, new GenerationState(), out _);
        }

        [Benchmark]
        public SkiaSharp.SKData Render()
        {
            return renderer.Render(diagram).Encode();
        }
    }
}
