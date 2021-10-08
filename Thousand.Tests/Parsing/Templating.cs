using System;
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
class foo($w) [min-width=$w]
";
            Assert.True(Parser.TryParse(source, warnings, errors, out var ast), errors.Join());

            Assert.Empty(ast!.Declarations.Where(d => d.IsT1));
        }

        [Fact]
        public void InstantiateObjectTemplate()
        {
            var source = @"
class foo($x) [min-width=$x]
foo(1) bar
";
            Assert.True(Parser.TryParse(source, warnings, errors, out var ast), errors.Join());

            var klass = (AST.ObjectClass)ast!.Declarations.Where(d => d.IsT1).First();

            Assert.Contains(new AST.NodeMinWidthAttribute(1), klass.Attributes);
        }

        [Fact]
        public void InstantiateLineTemplate()
        {
            var source = @"
class foo($s) [stroke=$s, anchor=none]
object a; object b
foo(2) a--b
";
            Assert.True(Parser.TryParse(source, warnings, errors, out var ast), errors.Join());

            var klass = (AST.LineClass)ast!.Declarations.Where(d => d.IsT1).First();

            Assert.Contains(new AST.LineStrokeAttribute(null, null, new PositiveWidth(2)), klass.Attributes);
        }

        [Fact]
        public void InstantiateTemplateTwice()
        {
            var source = @"
class foo($w) [min-width=$w]
foo(1) bar
foo(2) baz
";
            Assert.True(Parser.TryParse(source, warnings, errors, out var ast), errors.Join());

            var klasses = ast!.Declarations.Where(d => d.IsT1).Select(d => (AST.ObjectClass)d).ToList();
            Assert.Equal(2, klasses.Count);
            Assert.NotEqual(klasses[0].Name.Text, klasses[1].Name.Text);

            var objekts = ast!.Declarations.Where(d => d.IsT2).Select(d => (AST.TypedObject)d).ToList();
            Assert.Equal(2, objekts.Count);
            Assert.NotEqual(objekts[0].Classes[0].Text, objekts[1].Classes[0].Text);
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
            Assert.True(Parser.TryParse(source, warnings, errors, out var ast), errors.Join());

            var klass = (AST.ObjectClass)ast!.Declarations.Where(d => d.IsT1).First();

            Assert.Contains(new AST.NodeShapeAttribute(Enum.Parse<ShapeKind>(shape)), klass.Attributes);
        }

        [Fact]
        public void InstantiateTemplateMultipleArguments()
        {
            var source = @"
class foo($x, $y) [min-width=$x, min-height=$y]
foo(1, 2) bar
";
            Assert.True(Parser.TryParse(source, warnings, errors, out var ast), errors.Join());

            var klass = (AST.ObjectClass)ast!.Declarations.Where(d => d.IsT1).First();

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
            Assert.True(Parser.TryParse(source, warnings, errors, out var ast), errors.Join());
            // XXX more asserts
        }

        [Fact]
        public void InstantiateNestedTemplate()
        {
            var source = @"
class foo($x) [min-width=$x]
object { foo(1) }
";
            Assert.True(Parser.TryParse(source, warnings, errors, out var ast), errors.Join());

            var klass = (AST.ObjectClass)ast!.Declarations.Where(d => d.IsT1).First();

            Assert.Contains(new AST.NodeMinWidthAttribute(1), klass.Attributes);
        }

        [Fact]
        public void InstantiateDeeplyNestedTemplate()
        {
            var source = @"
class foo($x) [min-width=$x]
object { object { foo(1) } }
";
            Assert.True(Parser.TryParse(source, warnings, errors, out var ast), errors.Join());

            var klass = (AST.ObjectClass)ast!.Declarations.Where(d => d.IsT1).First();

            Assert.Contains(new AST.NodeMinWidthAttribute(1), klass.Attributes);
        }
    }
}
