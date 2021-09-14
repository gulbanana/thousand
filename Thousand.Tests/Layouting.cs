using Xunit;

namespace Thousand.Tests
{
    public class Composition
    {
        [Fact]
        public void CreateFromDiagram()
        {
            var diagram = new AST.Document(new AST.Node[] { new("foo"), new("bar") });
            var layout = Composer.Compose(diagram);

            Assert.Equal(200, layout.Width);
            Assert.Equal(100, layout.Height);
        }
    }
}
