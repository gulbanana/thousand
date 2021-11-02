using System;
using System.Linq;
using Thousand.Model;
using Thousand.Parse;
using Xunit;

namespace Thousand.Tests.Parsing
{
    public class Templating
    {
        private readonly GenerationState state = new();

        [Fact]
        public void RemoveUnreferencedTemplate()
        {
            var source = @"
class foo($w) [min-width=$w]
";
            Assert.True(Facade.TryParse(source, state, out var ast), state.JoinErrors());

            Assert.Empty(ast!.Declarations.OfType<AST.ObjectClass>());
        }

        [Fact]
        public void InstantiateObjectTemplate()
        {
            var source = @"
class foo($x) [min-width=$x]
foo(1) bar
";
            Assert.True(Facade.TryParse(source, state, out var ast), state.JoinErrors());

            var klass = ast!.Declarations.OfType<AST.ObjectClass>().First();

            Assert.Contains(new AST.NodeMinWidthAttribute(1), klass.Attributes);
        }

        [Fact]
        public void InstantiateLineTemplate()
        {
            var source = @"
class foo($s) [stroke=$s, anchor-start=none]
object a; object b
foo(2) a--b
";
            Assert.True(Facade.TryParse(source, state, out var ast), state.JoinErrors());

            var klass = ast!.Declarations.OfType<AST.LineClass>().First();

            Assert.Contains(new AST.EntityStrokeAttribute(null, null, new PositiveWidth(2)), klass.Attributes);
        }

        [Fact]
        public void InstantiateClassTemplate()
        {
            var source = @"
class foo($x) [min-width=$x]
class bar : foo(1)
bar baz
";
            Assert.True(Facade.TryParse(source, state, out var ast), state.JoinErrors());

            var klass = ast!.Declarations.OfType<AST.ObjectClass>().First();

            Assert.Contains(new AST.NodeMinWidthAttribute(1), klass.Attributes);
        }

        [Fact]
        public void InstantiateTemplateTemplate()
        {
            var source = @"
class foo($x) [min-width=$x]
class bar($y) : foo($y) [min-height=$y]
bar(1) baz
";
            Assert.True(Facade.TryParse(source, state, out var ast), state.JoinErrors());

            var foo = ast!.Declarations.OfType<AST.ObjectClass>().First();
            var bar = ast!.Declarations.OfType<AST.ObjectClass>().Last();

            Assert.Contains(new AST.NodeMinWidthAttribute(1), foo.Attributes);
            Assert.Contains(new AST.NodeMinHeightAttribute(1), bar.Attributes);
        }

        [Fact]
        public void InstantiateTemplateTemplateTemplate()
        {
            var source = @"
class foo($x) [min-width=$x]
class bar($y) : foo($y) [min-height=$y]
class baz($z) : bar($z)
baz(1) quux
";
            Assert.True(Facade.TryParse(source, state, out var ast), state.JoinErrors());

            var foo = ast!.Declarations.OfType<AST.ObjectClass>().ElementAt(0);
            var bar = ast!.Declarations.OfType<AST.ObjectClass>().ElementAt(1);

            Assert.Contains(new AST.NodeMinWidthAttribute(1), foo.Attributes);
            Assert.Contains(new AST.NodeMinHeightAttribute(1), bar.Attributes);
        }

        [Fact]
        public void InstantiateTemplateTwice()
        {
            var source = @"
class foo($w) [min-width=$w]
foo(1) bar
foo(2) baz
";
            Assert.True(Facade.TryParse(source, state, out var ast), state.JoinErrors());

            var klasses = ast!.Declarations.OfType<AST.ObjectClass>().ToList();
            Assert.Equal(2, klasses.Count);
            Assert.NotEqual(klasses[0].Name.AsKey, klasses[1].Name.AsKey);

            var objekts = ast!.Declarations.OfType<AST.TypedObject>().ToList();
            Assert.Equal(2, objekts.Count);
            Assert.NotEqual(objekts[0].Classes[0].AsKey, objekts[1].Classes[0].AsKey);
        }

        [Theory]
        [InlineData("Rect")]
        [InlineData("Ellipse")]
        [InlineData("Rhombus")]
        public void InstantiateTemplateVaryingArguments(string shape)
        {
            var source = @$"
class foo($shape) [shape=$shape]
foo({shape}) bar
";
            Assert.True(Facade.TryParse(source, state, out var ast), state.JoinErrors());

            var klass = ast!.Declarations.OfType<AST.ObjectClass>().First();

            Assert.Contains(new AST.NodeShapeAttribute(Enum.Parse<ShapeKind>(shape)), klass.Attributes);
        }

        [Fact]
        public void InstantiateTemplateMultipleArguments()
        {
            var source = @"
class foo($x, $y) [min-width=$x, min-height=$y]
foo(1, 2) bar
";
            Assert.True(Facade.TryParse(source, state, out var ast), state.JoinErrors());

            var klass = ast!.Declarations.OfType<AST.ObjectClass>().First();

            Assert.Contains(new AST.NodeMinWidthAttribute(1), klass.Attributes);
            Assert.Contains(new AST.NodeMinHeightAttribute(2), klass.Attributes);
        }

        [Fact]
        public void InstantiateMultipleTemplates()
        {
            var source = @"
class foo($x) [min-width=$x]
class bar($x) [min-height=$x]
foo(1).bar(2) bar
";
            Assert.True(Facade.TryParse(source, state, out var ast), state.JoinErrors());
            // XXX more asserts
        }

        [Fact]
        public void InstantiateNestedTemplate()
        {
            var source = @"
class foo($x) [min-width=$x]
object { foo(1) }
";
            Assert.True(Facade.TryParse(source, state, out var ast), state.JoinErrors());

            var klass = ast!.Declarations.OfType<AST.ObjectClass>().First();

            Assert.Contains(new AST.NodeMinWidthAttribute(1), klass.Attributes);
        }

        [Fact]
        public void InstantiateDeeplyNestedTemplate()
        {
            var source = @"
class foo($x) [min-width=$x]
object { object { foo(1) } }
";
            Assert.True(Facade.TryParse(source, state, out var ast), state.JoinErrors());

            var klass = ast!.Declarations.OfType<AST.ObjectClass>().First();

            Assert.Contains(new AST.NodeMinWidthAttribute(1), klass.Attributes);
        }

        [Fact]
        public void InstantiateTemplateWithBody()
        {
            var source = @"
class foo($x) {
}
foo("""")
";
            Assert.True(Facade.TryParse(source, state, out var ast), state.JoinErrors());

            var objekt = ast!.Declarations.OfType<AST.TypedObject>().FirstOrDefault();

            Assert.NotNull(objekt);
        }

        [Fact]
        public void InstantiateTemplateWithBodyUsingVariable()
        {
            var source = @"
class foo($x) {
    object $x
}
foo("""")
";
            Assert.True(Facade.TryParse(source, state, out var ast), state.JoinErrors());

            var objekt = ast!.Declarations.OfType<AST.TypedObject>().FirstOrDefault();

            Assert.NotNull(objekt);
        }

        [Fact]
        public void InstantiateTemplateWithDefaults()
        {
            var source = @"
class foo($x=100) [min-width=$x]
foo bar
";
            Assert.True(Facade.TryParse(source, state, out var ast), state.JoinErrors());

            var klass = ast!.Declarations.OfType<AST.ObjectClass>().First();

            Assert.Contains(new AST.NodeMinWidthAttribute(100), klass.Attributes);
        }
    }
}
