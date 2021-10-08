using System.Collections.Generic;
using System.Linq;
using Thousand.Model;
using Thousand.Parse;
using Xunit;

namespace Thousand.Tests.Parsing
{
    public class Templating
    {
        private readonly List<GenerationError> warnings = new();
        private readonly List<GenerationError> errors = new();

        [Fact]
        public void RemoveUnreferencedTemplate()
        {
            var source = @"
class foo() [min-width=$one, min-height=2, corner-radius=$three]
";
            Assert.True(Parser.TryParse(source, warnings, errors, out var ast), errors.Join());

            Assert.Empty(ast!.Declarations.Where(d => d.IsT1));
        }

        [Fact]
        public void InstantiateObjectTemplate()
        {
            var source = @"
class foo() [min-width=$one, min-height=3, corner-radius=$two]
foo bar
";
            Assert.True(Parser.TryParse(source, warnings, errors, out var ast), errors.Join());

            var klass = (AST.ObjectClass)ast!.Declarations.Where(d => d.IsT1).First();

            Assert.Contains(new AST.NodeMinWidthAttribute(1), klass.Attributes);
            Assert.Contains(new AST.NodeMinHeightAttribute(3), klass.Attributes);
            Assert.Contains(new AST.NodeCornerRadiusAttribute(2), klass.Attributes);
        }

        [Fact]
        public void InstantiateLineTemplate()
        {
            var source = @"
class foo() [stroke=$two, anchor=none]
object a; object b
foo a--b
";
            Assert.True(Parser.TryParse(source, warnings, errors, out var ast), errors.Join());

            var klass = (AST.LineClass)ast!.Declarations.Where(d => d.IsT1).First();

            Assert.Contains(new AST.LineStrokeAttribute(null, null, new PositiveWidth(2)), klass.Attributes);
        }

        [Fact]
        public void InstantiateReferencedTemplateTwice()
        {
            var source = @"
class foo() [min-width=$one, min-height=3, corner-radius=$two]
foo bar
foo baz
";
            Assert.True(Parser.TryParse(source, warnings, errors, out var ast), errors.Join());

            var klasses = ast!.Declarations.Where(d => d.IsT1).Select(d => (AST.ObjectClass)d).ToList();
            Assert.Equal(2, klasses.Count);
            Assert.NotEqual(klasses[0].Name.Text, klasses[1].Name.Text);

            var objekts = ast!.Declarations.Where(d => d.IsT2).Select(d => (AST.TypedObject)d).ToList();
            Assert.Equal(2, objekts.Count);
            Assert.NotEqual(objekts[0].Classes[0].Text, objekts[1].Classes[0].Text);
        }
    }
}
