using System.Collections.Generic;
using System.IO;
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

        [Theory, MemberData(nameof(Samples))]
        public void RenderSample(string filename)
        {
            var graph = File.ReadAllText(@"samples\" + filename);

            using var generator = new Render.SKDiagramGenerator();

            generator
                .GenerateImage(graph)
                .Switch(result => Assert.Empty(result.Warnings), errors => AssertEx.Fail(errors.Join()));            
        }
    }
}
