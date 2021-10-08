using System.Collections.Generic;
using System.IO;
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
            var ast = TokenParsers.TypedDocument(tokens);
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
    }
}
