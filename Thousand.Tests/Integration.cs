using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        [Fact]
        public void ParseStdlib()
        {
            var source = DiagramGenerator.ReadStdlib();
            var tokens = Tokenizer.Build().Tokenize(source);
            var ast = Typed.Document(tokens);
            Assert.True(ast.HasValue, ast.ToString());
        }

        [Theory, MemberData(nameof(Samples))]
        public void RenderSample(string filename)
        {
            var graph = File.ReadAllText(@"samples\" + filename);

            using var generator = new Render.SkiaDiagramGenerator();

            generator
                .GenerateImage(graph)
                .Switch(result => Assert.Empty(result.Warnings), errors => AssertEx.Fail(state.JoinErrors()));            
        }

        [Fact]
        public void EntityOffsetAsObject()
        {
            Assert.True(Parser.TryParse(@"class foo [offset=1 1]; foo bar", state, out var document), state.JoinErrors());
            Assert.True(Evaluator.TryEvaluate(new[] { document! }, state, out var rules), state.JoinErrors());

            var objekt = rules!.Region.Objects.Single();
            Assert.Equal(new Point(1, 1), objekt.Offset);
        }

        [Fact]
        public void EntityOffsetAsLine()
        {
            Assert.True(Parser.TryParse(@"class object; class foo [offset=1 1]; object bar; foo bar--bar", state, out var document), state.JoinErrors());
            Assert.True(Evaluator.TryEvaluate(new[] { document! }, state, out var rules), state.JoinErrors());

            var line = rules!.Edges.Single();
            Assert.Equal(new Point(1, 1), line.FromOffset);
            Assert.Equal(new Point(1, 1), line.ToOffset);
        }

        [Fact]
        public void LineOffsetAsLine()
        {
            Assert.True(Parser.TryParse(@"class object; class foo [offset=1 1 2 2]; object bar; foo bar--bar", state, out var document), state.JoinErrors());
            Assert.True(Evaluator.TryEvaluate(new[] { document! }, state, out var rules), state.JoinErrors());

            var line = rules!.Edges.Single();
            Assert.Equal(new Point(1, 1), line.FromOffset);
            Assert.Equal(new Point(2, 2), line.ToOffset);
        }

        [Fact]
        public void EntityAnchorAsObject()
        {
            Assert.True(Parser.TryParse(@"class foo [anchor=n]; foo bar", state, out var document), state.JoinErrors());
            Assert.True(Evaluator.TryEvaluate(new[] { document! }, state, out var rules), state.JoinErrors());

            var objekt = rules!.Region.Objects.Single();
            Assert.Equal(CompassKind.N, objekt.Anchor);
        }

        [Fact]
        public void EntityAnchorAsLine()
        {
            Assert.True(Parser.TryParse(@"class object; class foo [anchor=n]; object bar; foo bar--bar", state, out var document), state.JoinErrors());
            Assert.True(Evaluator.TryEvaluate(new[] { document! }, state, out var rules), state.JoinErrors());

            var line = rules!.Edges.Single();
            Assert.Equal(new SpecificAnchor(CompassKind.N), line.FromAnchor);
            Assert.Equal(new SpecificAnchor(CompassKind.N), line.ToAnchor);
        }

        [Fact]
        public void LineAnchorAsLine_Twice()
        {
            Assert.True(Parser.TryParse(@"class object; class foo [anchor=n e]; object bar; foo bar--bar", state, out var document), state.JoinErrors());
            Assert.True(Evaluator.TryEvaluate(new[] { document! }, state, out var rules), state.JoinErrors());

            var line = rules!.Edges.Single();
            Assert.Equal(new SpecificAnchor(CompassKind.N), line.FromAnchor);
            Assert.Equal(new SpecificAnchor(CompassKind.E), line.ToAnchor);
        }

        [Fact]
        public void LineAnchorAsLine_Group()
        {
            Assert.True(Parser.TryParse(@"class object; class foo [anchor=any]; object bar; foo bar--bar", state, out var document), state.JoinErrors());
            Assert.True(Evaluator.TryEvaluate(new[] { document! }, state, out var rules), state.JoinErrors());

            var line = rules!.Edges.Single();
            Assert.Equal(new AnyAnchor(), line.FromAnchor);
            Assert.Equal(new AnyAnchor(), line.ToAnchor);
        }
    }
}
