using BenchmarkDotNet.Attributes;
using SkiaSharp;
using System;
using System.IO;
using System.Xml.Linq;
using Thousand.API;
using Thousand.Compose;
using Thousand.Evaluate;
using Thousand.Parse;
using Thousand.Render;

namespace Thousand.Benchmarks
{
    public class Outputs
    {
        [Params("connectors.1000", "tetris.1000", "underground.1000")]
        public string Input { get; set; } = default!;

        private readonly SkiaRenderer skia;
        private readonly SVGRenderer svg;
        private string source;
        private AST.TypedDocument stdlib;
        private AST.TypedDocument ast;
        private IR.Region root;
        private Layout.Diagram diagram;
        
        public Outputs()
        {
            skia = new SkiaRenderer();
            svg = new SVGRenderer();
        }

        [GlobalSetup]
        public void Setup()
        {
            source = File.ReadAllText(Input);

            var api = new Metadata();
            var state = new GenerationState();

            var stdlibSource = DiagramGenerator.ReadStdlib();
            Preprocessor.TryPreprocess(state, stdlibSource, out var stdlibMacros);
            Typechecker.TryTypecheck(api, state, stdlibMacros, Shared.GetEnd(stdlibSource), false, out stdlib);

            Preprocessor.TryPreprocess(state, source, out var macros);
            Typechecker.TryTypecheck(api, state, macros, Shared.GetEnd(source), false, out ast);
            Evaluator.TryEvaluate(new[] { stdlib, ast }, state, out root);
            Composer.TryCompose(root, state, out diagram);

            if (state.HasErrors())
            {
                throw new Exception("source invalid, cannot benchmark");
            }
        }

        [Benchmark]
        public SkiaSharp.SKData PNG() // JPG is nearly identical
        {
            return skia.Render(diagram).Encode(SKEncodedImageFormat.Png, 100);
        }

        [Benchmark]
        public XElement XML()
        {
            return svg.Render(diagram);
        }

        [Benchmark]
        public string SVG()
        {
            return svg.Render(diagram).ToString();
        }
    }
}
