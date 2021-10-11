using System.Collections.Generic;
using System.IO;
using System.Linq;
using Thousand.Model;
using Thousand.Parse;
using Xunit;

namespace Thousand.Tests
{
    public class Integration
    {
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
                .Switch(result => Assert.Empty(result.Warnings), errors => AssertEx.Fail(errors.Join()));            
        }

        [Fact]
        public void EntityOffsetAsObject()
        {
            var warnings = new List<GenerationError>();
            var errors = new List<GenerationError>();
            Assert.True(Parser.TryParse(@"class foo [offset=1 1]; foo bar", warnings, errors, out var document), errors.Join());
            Assert.True(Evaluator.TryEvaluate(new[] { document! }, warnings, errors, out var rules), errors.Join());
            Assert.Empty(warnings);

            var objekt = rules!.Region.Objects.Single();
            Assert.Equal(new Point(1, 1), objekt.Offset);
        }

        [Fact]
        public void EntityOffsetAsLine()
        {
            var warnings = new List<GenerationError>();
            var errors = new List<GenerationError>();
            Assert.True(Parser.TryParse(@"class object; class foo [offset=1 1]; object bar; foo bar--bar", warnings, errors, out var document), errors.Join());
            Assert.True(Evaluator.TryEvaluate(new[] { document! }, warnings, errors, out var rules), errors.Join());
            Assert.Empty(warnings);

            var line = rules!.Edges.Single();
            Assert.Equal(new Point(1, 1), line.FromOffset);
            Assert.Equal(new Point(1, 1), line.ToOffset);
        }

        [Fact]
        public void LineOffsetAsLine()
        {
            var warnings = new List<GenerationError>();
            var errors = new List<GenerationError>();
            Assert.True(Parser.TryParse(@"class object; class foo [offset=1 1 2 2]; object bar; foo bar--bar", warnings, errors, out var document), errors.Join());
            Assert.True(Evaluator.TryEvaluate(new[] { document! }, warnings, errors, out var rules), errors.Join());
            Assert.Empty(warnings);

            var line = rules!.Edges.Single();
            Assert.Equal(new Point(1, 1), line.FromOffset);
            Assert.Equal(new Point(2, 2), line.ToOffset);
        }

        [Fact]
        public void EntityAnchorAsObject()
        {
            var warnings = new List<GenerationError>();
            var errors = new List<GenerationError>();
            Assert.True(Parser.TryParse(@"class foo [anchor=n]; foo bar", warnings, errors, out var document), errors.Join());
            Assert.True(Evaluator.TryEvaluate(new[] { document! }, warnings, errors, out var rules), errors.Join());
            Assert.Empty(warnings);

            var objekt = rules!.Region.Objects.Single();
            Assert.Equal(CompassKind.N, objekt.Anchor);
        }

        [Fact]
        public void EntityAnchorAsLine()
        {
            var warnings = new List<GenerationError>();
            var errors = new List<GenerationError>();
            Assert.True(Parser.TryParse(@"class object; class foo [anchor=n]; object bar; foo bar--bar", warnings, errors, out var document), errors.Join());
            Assert.True(Evaluator.TryEvaluate(new[] { document! }, warnings, errors, out var rules), errors.Join());
            Assert.Empty(warnings);

            var line = rules!.Edges.Single();
            Assert.Equal(new SpecificAnchor(CompassKind.N), line.FromAnchor);
            Assert.Equal(new SpecificAnchor(CompassKind.N), line.ToAnchor);
        }

        [Fact]
        public void LineAnchorAsLine_Twice()
        {
            var warnings = new List<GenerationError>();
            var errors = new List<GenerationError>();
            Assert.True(Parser.TryParse(@"class object; class foo [anchor=n e]; object bar; foo bar--bar", warnings, errors, out var document), errors.Join());
            Assert.True(Evaluator.TryEvaluate(new[] { document! }, warnings, errors, out var rules), errors.Join());
            Assert.Empty(warnings);

            var line = rules!.Edges.Single();
            Assert.Equal(new SpecificAnchor(CompassKind.N), line.FromAnchor);
            Assert.Equal(new SpecificAnchor(CompassKind.E), line.ToAnchor);
        }

        [Fact]
        public void LineAnchorAsLine_Group()
        {
            var warnings = new List<GenerationError>();
            var errors = new List<GenerationError>();
            Assert.True(Parser.TryParse(@"class object; class foo [anchor=any]; object bar; foo bar--bar", warnings, errors, out var document), errors.Join());
            Assert.True(Evaluator.TryEvaluate(new[] { document! }, warnings, errors, out var rules), errors.Join());
            Assert.Empty(warnings);

            var line = rules!.Edges.Single();
            Assert.Equal(new AnyAnchor(), line.FromAnchor);
            Assert.Equal(new AnyAnchor(), line.ToAnchor);
        }
    }
}
