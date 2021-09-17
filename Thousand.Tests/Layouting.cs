using System;
using Xunit;

namespace Thousand.Tests
{
    public class Composition
    {
        [Fact]
        public void CreateFromDiagram()
        {
            var diagram = new AST.Document(new AST.Node[] { new("foo", Array.Empty<AST.NodeAttribute>()), new("bar", Array.Empty<AST.NodeAttribute>()) });
            var layout = Composer.Compose(diagram);

            Assert.Equal(300, layout.Width);
            Assert.Equal(150, layout.Height);
        }
    }
}
