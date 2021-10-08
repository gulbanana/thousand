using System.Linq;
using Thousand.Parse;
using Xunit;

namespace Thousand.Tests.Parsing
{
    public class Templating
    {
        [Fact]
        public void IntegrationTest()
        {
            var source = @"
class foo [min-width=$one, min-height=2, corner-radius=$three]
foo bar
";
            Assert.True(Parser.TryParse(source, new(), new(), out var ast));

            var klass = (AST.ObjectClass)ast!.Declarations.First();

            Assert.Contains(new AST.NodeMinWidthAttribute(1), klass.Attributes);
            Assert.Contains(new AST.NodeMinHeightAttribute(2), klass.Attributes);
            Assert.Contains(new AST.NodeCornerRadiusAttribute(3), klass.Attributes);
        }
    }
}
