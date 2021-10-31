using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Thousand.Evaluate;
using Thousand.Model;
using Thousand.Parse;
using Xunit;

namespace Thousand.Tests
{
    public class Integration : IDisposable
    {
        private readonly GenerationState state = new();

        public void Dispose()
        {
            Assert.False(state.HasWarnings(), state.JoinWarnings());
        }

        private static IEnumerable<object[]> Samples()
        {
            foreach (var filename in Directory.GetFiles("samples"))
            {
                yield return new object[] {Path.GetFileName(filename) };
            }
        }

        private static IEnumerable<object[]> Benchmarks()
        {
            foreach (var filename in Directory.GetFiles("benchmarks"))
            {
                yield return new object[] { Path.GetFileName(filename) };
            }
        }

        [Fact]
        public void ParseStdlib()
        {
            var source = DiagramGenerator.ReadStdlib();
            var tokens = new Tokenizer().Tokenize(source);
            var ast = Typed.Document(tokens);
            Assert.True(ast.HasValue, ast.ToString());
        }

        [Theory, MemberData(nameof(Samples))]
        public void RenderSample(string filename)
        {
            var graph = File.ReadAllText(@"samples\" + filename);

            using var generator = new DiagramGenerator<SkiaSharp.SKImage>(new Render.SkiaRenderer());

            generator
                .GenerateImage(graph)
                .Switch(result => Assert.Empty(result.Warnings), errors => AssertEx.Fail(errors.First().ToString()));            
        }

        [Theory, MemberData(nameof(Benchmarks))]
        public void RenderBenchmark(string filename)
        {
            var graph = File.ReadAllText(@"benchmarks\" + filename);

            using var generator = new DiagramGenerator<SkiaSharp.SKImage>(new Render.SkiaRenderer());

            generator
                .GenerateImage(graph)
                .Switch(result => Assert.Empty(result.Warnings), errors => AssertEx.Fail(errors.First().ToString()));
        }

        [Fact]
        public void EntityOffsetAsObject()
        {
            Assert.True(Facade.TryParse(@"class foo [offset=1 1]; foo bar", state, out var document), state.JoinErrors());
            Assert.True(Evaluator.TryEvaluate(new[] { document! }, state, out var root), state.JoinErrors());

            var objekt = root!.Objects.Single();
            Assert.Equal(new Point(1, 1), objekt.Offset);
        }

        [Fact]
        public void EntityOffsetAsLine()
        {
            Assert.True(Facade.TryParse(@"class object; class foo [offset=1 1]; object bar; foo bar--bar", state, out var document), state.JoinErrors());
            Assert.True(Evaluator.TryEvaluate(new[] { document! }, state, out var root), state.JoinErrors());

            var line = root!.Edges.Single();
            Assert.Equal(new Point(1, 1), line.From.Offset);
            Assert.Equal(new Point(1, 1), line.To.Offset);
        }

        [Fact]
        public void LineOffsetAsLine()
        {
            Assert.True(Facade.TryParse(@"class object; class foo [offset=1 1 2 2]; object bar; foo bar--bar", state, out var document), state.JoinErrors());
            Assert.True(Evaluator.TryEvaluate(new[] { document! }, state, out var root), state.JoinErrors());

            var line = root!.Edges.Single();
            Assert.Equal(new Point(1, 1), line.From.Offset);
            Assert.Equal(new Point(2, 2), line.To.Offset);
        }

        [Fact]
        public void EntityAnchorAsObject()
        {
            Assert.True(Facade.TryParse(@"class foo [anchor=n]; foo bar", state, out var document), state.JoinErrors());
            Assert.True(Evaluator.TryEvaluate(new[] { document! }, state, out var root), state.JoinErrors());

            var objekt = root!.Objects.Single();
            Assert.Equal(CompassKind.N, objekt.Anchor);
        }

        [Fact]
        public void EntityAnchorAsLine()
        {
            Assert.True(Facade.TryParse(@"class object; class foo [anchor=n]; object bar; foo bar--bar", state, out var document), state.JoinErrors());
            Assert.True(Evaluator.TryEvaluate(new[] { document! }, state, out var root), state.JoinErrors());

            var line = root!.Edges.Single();
            Assert.Equal(new SpecificAnchor(CompassKind.N), line.From.Anchor);
            Assert.Equal(new SpecificAnchor(CompassKind.N), line.To.Anchor);
        }

        [Fact]
        public void LineAnchorAsLine_Twice()
        {
            Assert.True(Facade.TryParse(@"class object; class foo [anchor=n e]; object bar; foo bar--bar", state, out var document), state.JoinErrors());
            Assert.True(Evaluator.TryEvaluate(new[] { document! }, state, out var root), state.JoinErrors());

            var line = root!.Edges.Single();
            Assert.Equal(new SpecificAnchor(CompassKind.N), line.From.Anchor);
            Assert.Equal(new SpecificAnchor(CompassKind.E), line.To.Anchor);
        }

        [Fact]
        public void LineAnchorAsLine_Group()
        {
            Assert.True(Facade.TryParse(@"class object; class foo [anchor=any]; object bar; foo bar--bar", state, out var document), state.JoinErrors());
            Assert.True(Evaluator.TryEvaluate(new[] { document! }, state, out var root), state.JoinErrors());

            var line = root!.Edges.Single();
            Assert.Equal(new AnyAnchor(), line.From.Anchor);
            Assert.Equal(new AnyAnchor(), line.To.Anchor);
        }

        [Fact]
        public void Regression_MultiwordEntityAttribute_UsingASTTypechecker()
        {
            Assert.True(Facade.TryParse(@"class x [stroke-colour=black]", state, out var document), state.JoinErrors());
        }

        [Fact]
        public void Regression_DeclarationTrailersAreErrors()
        {
            Facade.TryParse(@"object foo; object bar {} baz", state, out var doc);
            Assert.True(state.HasErrors());
        }
    }
}
