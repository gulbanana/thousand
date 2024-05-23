using Superpower.Model;
using System;
using System.Linq;
using Thousand.Model;
using Thousand.Parse;
using Xunit;

namespace Thousand.Tests.Parsing
{
    public class Misc
    {
        private readonly Superpower.Tokenizer<TokenKind> tokenizer;

        public Misc()
        {
            tokenizer = new Tokenizer();
        }

        [Fact]
        public void AttributeList_Single()
        {
            var tokens = tokenizer.Tokenize(@"[shape=square]");
            var result = Typed.AttributeList(Typed.NodeAttribute)(tokens);

            Assert.True(result.HasValue, result.ToString());
            AssertEx.Sequence(result.Value, new AST.NodeShapeAttribute(ShapeKind.Square));
        }

        [Fact]
        public void AttributeList_Multiple()
        {
            var tokens = tokenizer.Tokenize(@"[shape=square,shape=oval]");
            var result = Typed.AttributeList(Typed.NodeAttribute)(tokens);

            Assert.True(result.HasValue, result.ToString());
            AssertEx.Sequence(result.Value, new AST.NodeShapeAttribute(ShapeKind.Square), new AST.NodeShapeAttribute(ShapeKind.Ellipse));
        }

        [Fact]
        public void AttributeList_Whitespace()
        {
            var tokens = tokenizer.Tokenize(@"[ shape=square,shape = square, shape=square]");
            var result = Typed.AttributeList(Typed.NodeAttribute)(tokens);

            Assert.True(result.HasValue, result.ToString());
            AssertEx.Sequence(result.Value, new AST.NodeShapeAttribute(ShapeKind.Square), new AST.NodeShapeAttribute(ShapeKind.Square), new AST.NodeShapeAttribute(ShapeKind.Square));
        }

        [Fact]
        public void AttributeList_Incomplete()
        {
            var tokens = tokenizer.Tokenize(@"[shape=square;[other=stuff]");
            var result = Untyped.AttributeList(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.False(result.Value.IsComplete.Value);
            Assert.Equal(13, result.Value.IsComplete.Span(TextSpan.None).Position.Absolute);
        }

        [Fact]
        public void AttributeList_Incomplete_InDocument()
        {
            var tokens = tokenizer.Tokenize(@"object [shape=square
object [shape=square]");
            var result = Untyped.Document(tokens);

            Assert.True(result.HasValue, result.ToString());
            var list = (result.Value.Declarations.First().Value as AST.UntypedObject)!.Attributes;

            Assert.False(list.IsComplete.Value);
            Assert.Equal(20, list.IsComplete.Span(TextSpan.None).Position.Absolute);
        }

        [Fact]
        public void Object()
        {
            var tokens = tokenizer.Tokenize(@"object foo");
            var result = Typed.Object(tokens);

            Assert.True(result.HasValue, result.ToString());
            AssertEx.Sequence(result.Value.Classes.Select(n => n.AsKey), "object");
            Assert.Equal("foo", result.Value.Name?.AsKey);
        }

        [Fact]
        public void Object_WhiteSpace()
        {
            var tokens = tokenizer.Tokenize(@"   object     foo    ");
            var result = Typed.Object(tokens);

            Assert.True(result.HasValue, result.ToString());
            AssertEx.Sequence(result.Value.Classes.Select(n => n.AsKey), "object");
            Assert.Equal("foo", result.Value.Name?.AsKey);
        }

        [Fact]
        public void Object_Multiline()
        {
            var tokens = tokenizer.Tokenize(@"object ""foo
bar""");
            var result = Typed.Object(tokens);

            Assert.True(result.HasValue, result.ToString());
            AssertEx.Sequence(result.Value.Classes.Select(n => n.AsKey), "object");
            Assert.Equal(@"foo
bar", result.Value.Name?.AsKey);
        }

        [Fact]
        public void Object_Attributed()
        {
            var tokens = tokenizer.Tokenize(@"object ""foo"" [label-content=""bar""]");
            var result = Typed.Object(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.Equal("foo", result.Value.Name?.AsKey);
            AssertEx.Sequence(result.Value.Attributes, new AST.EntityLabelContentAttribute("bar"));
        }

        [Fact]
        public void Object_CustomClass()
        {
            var tokens = tokenizer.Tokenize(@"foo bar");
            var result = Typed.Object(tokens);

            Assert.True(result.HasValue, result.ToString());
            AssertEx.Sequence(result.Value.Classes.Select(n => n.AsKey), "foo");
            Assert.Equal("bar", result.Value.Name?.AsKey);
        }

        [Fact]
        public void Object_CustomClasses()
        {
            var tokens = tokenizer.Tokenize(@"foo.bar baz");
            var result = Typed.Object(tokens);

            Assert.True(result.HasValue, result.ToString());
            AssertEx.Sequence(result.Value.Classes.Select(n => n.AsKey), "foo", "bar");
            Assert.Equal("baz", result.Value.Name?.AsKey);
        }

        [Fact]
        public void Object_Anonymous()
        {
            var tokens = tokenizer.Tokenize(@"object");
            var result = Typed.Object(tokens);

            AssertEx.Sequence(result.Value.Classes.Select(n => n.AsKey), "object");
            Assert.True(result.HasValue, result.ToString());
        }


        [Fact]
        public void Object_TypeSpan()
        {
            var tokens = tokenizer.Tokenize(@"foo bar");
            var result = Untyped.Object(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.Equal("foo", result.Value.Classes.First()?.Value?.Name.AsKey);
            Assert.Equal(3, result.Value.Classes.First()?.SpanOrEmpty().Length);
        }

        [Fact]
        public void Line_Bare()
        {
            var tokens = tokenizer.Tokenize(@"""foo"" -> ""bar""");
            var result = Shared.LineSegments(Typed.ObjectFactory)(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.Equal("foo", result.Value.First().Target.AsT0.AsKey);
            Assert.Equal("bar", result.Value.Last().Target.AsT0.AsKey);
        }

        [Fact]
        public void Line_Untyped()
        {
            var tokens = tokenizer.Tokenize(@"line(x) foo--bar");
            var result = Untyped.Line(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.Equal("line", result.Value.Classes[0].Value?.Name.AsKey);
            Assert.Single(result.Value.Classes[0].Value?.Arguments);
        }

        [Fact]
        public void Line_Typed()
        {
            var tokens = tokenizer.Tokenize(@"line ""foo"" -> ""bar""");
            var result = Typed.Line(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.Equal("line", result.Value.Classes[0].AsKey);
        }

        [Fact]
        public void Line_Typed_WithAttributes()
        {
            var tokens = tokenizer.Tokenize(@"line ""foo"" -> ""bar"" [stroke-colour=#000000]");
            var result = Typed.Line(tokens);

            Assert.True(result.HasValue, result.ToString());
            AssertEx.Sequence(result.Value.Attributes, new AST.EntityStrokeColourAttribute(new Colour(0, 0, 0)));
        }

        [Fact]
        public void Line_WithInline()
        {
            var tokens = tokenizer.Tokenize(@"line (object foo) -> bar");
            var result = Typed.Line(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.True(result.Value.Segments.First().Target.IsT1);
        }

        [Fact]
        public void Line_WithInline_Untyped()
        {
            var tokens = tokenizer.Tokenize(@"line (object foo) -> bar");
            var result = Untyped.Line(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.True(result.Value.Segments.First().Target.IsT1);
        }

        [Fact]
        public void Line_WithInline_MultiSeg()
        {
            var tokens = tokenizer.Tokenize(@"line (object ""Lamp doesn't work"") -> ""Lamp\nplugged in?"" -> ""Bulb\nburned out?"" -> ""Repair Lamp""");
            var result = Typed.Line(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.True(result.Value.Segments.ElementAt(0).Target.IsT1);
            Assert.True(result.Value.Segments.ElementAt(1).Target.IsT0);
        }

        [Fact]
        public void Scope_Empty()
        {
            var tokens = tokenizer.Tokenize(@"{}");
            var result = Typed.DeclarationScope(Typed.Declaration)(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.Empty(result.Value);
        }

        [Fact]
        public void Scope_Bare()
        {
            var tokens = tokenizer.Tokenize(@"{
	object foo
    object bar 
    line foo <- bar
}");
            var result = Typed.DeclarationScope(Typed.Declaration)(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.Equal(3, result.Value.Count);
        }

        [Fact]
        public void Scope_InContext()
        {
            var tokens = tokenizer.Tokenize(@"object {
    class foo
	object bar
    object baz
    line bar <- baz
}");
            var result = Typed.Object(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.Equal(4, result.Value.Declarations.Count);
        }

        [Fact]
        public void Document_SingleNode()
        {
            var tokens = tokenizer.Tokenize(@"object ""foo""");
            var result = Typed.Document(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.Single(result.Value.Declarations);
            Assert.IsType<AST.TypedObject>(result.Value.Declarations.Single());
        }

        [Fact]
        public void Document_MultiNode()
        {
            var tokens = tokenizer.Tokenize(@"object ""foo""
object ""bar""
object ""baz""");

            var result = Typed.Document(tokens);

            Assert.True(result.HasValue, result.ToString());
            AssertEx.Sequence(result.Value.Declarations.OfType<AST.TypedObject>().Select(n => n.Name?.AsKey), "foo", "bar", "baz");
        }

        [Fact]
        public void Document_EmptyLines()
        {
            var tokens = tokenizer.Tokenize(@"object ""foo""

object ""bar""");

            var result = Typed.Document(tokens);

            Assert.True(result.HasValue, result.ToString());
            AssertEx.Sequence(result.Value.Declarations.OfType<AST.TypedObject>().Select(n => n.Name?.AsKey), "foo", "bar");
        }

        [Fact]
        public void Document_EmptyLines_Untyped()
        {
            var tokens = tokenizer.Tokenize(@"class object
");
            var result = Untyped.Document(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.Equal(2, result.Value.Declarations.Count);
        }

        [Fact]
        public void Document_EmptyLines_Spans()
        {
            var tokens = tokenizer.Tokenize(@"class object

");
            var result = Untyped.Document(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.Equal(3, result.Value.Declarations.Count);
            Assert.Equal(3, result.Value.Declarations[1].Location.Position);
            Assert.Equal(4, result.Value.Declarations[2].Location.Position);
        }

        [Fact]
        public void Document_SingleSeparatedLine()
        {
            var tokens = tokenizer.Tokenize(@"object ""foo""; object ""bar""");

            var result = Typed.Document(tokens);

            Assert.True(result.HasValue, result.ToString());
            AssertEx.Sequence(result.Value.Declarations.OfType<AST.TypedObject>().Select(n => n.Name?.AsKey), "foo", "bar");
        }

        [Fact]
        public void Document_NodesAndEdge()
        {
            var tokens = tokenizer.Tokenize(@"object foo
object bar
line foo -> bar");
            var result = Typed.Document(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.Collection(result.Value.Declarations,
                d => Assert.IsType<AST.TypedObject>(d),
                d => Assert.IsType<AST.TypedObject>(d),
                d => Assert.IsType<AST.TypedLine>(d)
            );
        }

        [Fact]
        public void Declaration_Class()
        {
            var tokens = tokenizer.Tokenize(@"class foo [stroke=none]");
            var result = Typed.Declaration(tokens);

            Assert.True(result.HasValue);
            Assert.IsType<AST.ObjectOrLineClass>(result.Value);
        }

        [Fact]
        public void Declaration_Object()
        {
            var tokens = tokenizer.Tokenize(@"object foo [shape=square]");
            var result = Typed.Declaration(tokens);

            Assert.True(result.HasValue);
            Assert.IsType<AST.TypedObject>(result.Value);
        }

        [Fact]
        public void Declaration_AnonymousObject()
        {
            var tokens = tokenizer.Tokenize(@"object {}");
            var result = Typed.Declaration(tokens);

            Assert.True(result.HasValue);
            Assert.IsType<AST.TypedObject>(result.Value);
        }

        [Fact]
        public void Declaration_WhollyAnonymousObject()
        {
            var tokens = tokenizer.Tokenize(@"object");
            var result = Typed.Declaration(tokens);

            Assert.True(result.HasValue);
            Assert.IsType<AST.TypedObject>(result.Value);
        }

        [Fact]
        public void Declaration_Line()
        {
            var tokens = tokenizer.Tokenize(@"line foo->bar [stroke=none]");
            var result = Typed.Declaration(tokens);

            Assert.True(result.HasValue);
            Assert.IsType<AST.TypedLine>(result.Value);
        }

        [Fact]
        public void Declaration_All()
        {
            var tokens = tokenizer.Tokenize(@"class foo [stroke=none]
object foo [shape=square]
line foo->bar [offset=1 1]");
            var result = Typed.Document(tokens);

            Assert.True(result.HasValue);
            Assert.Equal(3, result.Value.Declarations.Count);
            Assert.IsAssignableFrom<AST.TypedClass>(result.Value.Declarations[0]);
            Assert.IsType<AST.TypedObject>(result.Value.Declarations[1]);
            Assert.IsType<AST.TypedLine>(result.Value.Declarations[2]);
        }

        [Fact]
        public void Declaration_BlankLines()
        {
            var tokens = tokenizer.Tokenize(@"class foo [stroke=none]


object bar");
            var result = Typed.Document(tokens);

            Assert.True(result.HasValue);
            Assert.Equal(2, result.Value.Declarations.Count);
            Assert.IsAssignableFrom<AST.TypedClass>(result.Value.Declarations[0]);
            Assert.IsType<AST.TypedObject>(result.Value.Declarations[1]);
        }

        [Fact]
        public void Declaration_Untyped_Object()
        {
            var tokens = tokenizer.Tokenize(@"object foo");
            var result = Untyped.Document(tokens);

            Assert.True(result.HasValue);
            Assert.Single(result.Value.Declarations);
            Assert.Single(result.Value.Declarations.OfType<IMacro<AST.UntypedObject>>());
        }

        [Fact]
        public void Declaration_Untyped_Empty()
        {
            var tokens = tokenizer.Tokenize(@"");
            var result = Untyped.Document(tokens);

            Assert.True(result.HasValue);
            Assert.Single(result.Value.Declarations);
            Assert.Single(result.Value.Declarations.OfType<IMacro<AST.EmptyDeclaration>>());
        }

        [Fact]
        public void Declaration_Untyped_Invalid()
        {
            var tokens = tokenizer.Tokenize(@"object foo bar");
            var result = Untyped.Document(tokens);

            Assert.True(result.HasValue);
            Assert.Single(result.Value.Declarations);
            Assert.Single(result.Value.Declarations.OfType<IMacro<AST.InvalidDeclaration>>());
        }

        [Fact]
        public void Declaration_Untyped_Invalid_Multiline()
        {
            var tokens = tokenizer.Tokenize(@"object foo {
} bar");
            var result = Untyped.Document(tokens);

            Assert.True(result.HasValue);
            Assert.Single(result.Value.Declarations);
            Assert.Single(result.Value.Declarations.OfType<IMacro<AST.InvalidDeclaration>>());
        }
    }
}
