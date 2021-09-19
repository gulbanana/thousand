using System;
using Xunit;

namespace Thousand.Tests
{
    public class Composition
    {
        [Fact]
        public void CreateFromDiagram()
        {
            var document = new AST.Document(new AST.Node[] { new("foo", Array.Empty<AST.NodeAttribute>()), new("bar", Array.Empty<AST.NodeAttribute>()) });
            var result = Composer.TryCompose(document, out var layout, out var warnings, out var errors);

            Assert.True(result, string.Join(Environment.NewLine, errors));
            Assert.Equal(300, layout!.Width);
            Assert.Equal(150, layout.Height);
        }
    }
}
