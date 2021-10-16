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
            tokenizer = Tokenizer.Build();            
        }

        [Fact]
        public void AttributeList_Single()
        {
            var tokens = tokenizer.Tokenize(@"[shape=square]");
            var result = Shared.List(AttributeParsers.NodeAttribute)(tokens);

            Assert.True(result.HasValue, result.ToString());
            AssertEx.Sequence(result.Value, new AST.NodeShapeAttribute(ShapeKind.Square));
        }

        [Fact]
        public void AttributeList_Multiple()
        {
            var tokens = tokenizer.Tokenize(@"[shape=square,shape=oval]");
            var result = Shared.List(AttributeParsers.NodeAttribute)(tokens);

            Assert.True(result.HasValue, result.ToString());
            AssertEx.Sequence(result.Value, new AST.NodeShapeAttribute(ShapeKind.Square), new AST.NodeShapeAttribute(ShapeKind.Ellipse));
        }

        [Fact]
        public void AttributeList_Whitespace()
        {
            var tokens = tokenizer.Tokenize(@"[ shape=square,shape = square, shape=square]");
            var result = Shared.List(AttributeParsers.NodeAttribute)(tokens);

            Assert.True(result.HasValue, result.ToString());
            AssertEx.Sequence(result.Value, new AST.NodeShapeAttribute(ShapeKind.Square), new AST.NodeShapeAttribute(ShapeKind.Square), new AST.NodeShapeAttribute(ShapeKind.Square));
        }

        [Fact]
        public void Object()
        {
            var tokens = tokenizer.Tokenize(@"object foo");
            var result = Typed.Object(tokens);

            Assert.True(result.HasValue, result.ToString());
            AssertEx.Sequence(result.Value.Classes.Select(n => n.Text), "object");
            Assert.Equal("foo", result.Value.Name?.Text);
        }

        [Fact]
        public void Object_WhiteSpace()
        {
            var tokens = tokenizer.Tokenize(@"   object     foo    ");
            var result = Typed.Object(tokens);

            Assert.True(result.HasValue, result.ToString());
            AssertEx.Sequence(result.Value.Classes.Select(n => n.Text), "object");
            Assert.Equal("foo", result.Value.Name?.Text);
        }

        [Fact]
        public void Object_Multiline()
        {
            var tokens = tokenizer.Tokenize(@"object ""foo
bar""");
            var result = Typed.Object(tokens);

            Assert.True(result.HasValue, result.ToString());
            AssertEx.Sequence(result.Value.Classes.Select(n => n.Text), "object");
            Assert.Equal("foo" + Environment.NewLine + "bar", result.Value.Name?.Text);
        }

        [Fact]
        public void Object_Attributed()
        {
            var tokens = tokenizer.Tokenize(@"object ""foo"" [label-content=""bar""]");
            var result = Typed.Object(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.Equal("foo", result.Value.Name?.Text);
            AssertEx.Sequence(result.Value.Attributes, new AST.NodeLabelContentAttribute(new Text("bar")));
        }

        [Fact]
        public void Object_CustomClass()
        {
            var tokens = tokenizer.Tokenize(@"foo bar");
            var result = Typed.Object(tokens);

            Assert.True(result.HasValue, result.ToString());
            AssertEx.Sequence(result.Value.Classes.Select(n => n.Text), "foo");
            Assert.Equal("bar", result.Value.Name?.Text);
        }

        [Fact]
        public void Object_CustomClasses()
        {
            var tokens = tokenizer.Tokenize(@"foo.bar baz");
            var result = Typed.Object(tokens);

            Assert.True(result.HasValue, result.ToString());
            AssertEx.Sequence(result.Value.Classes.Select(n => n.Text), "foo", "bar");
            Assert.Equal("baz", result.Value.Name?.Text);
        }

        [Fact]
        public void Object_Anonymous()
        {
            var tokens = tokenizer.Tokenize(@"object");
            var result = Typed.Object(tokens);

            AssertEx.Sequence(result.Value.Classes.Select(n => n.Text), "object");
            Assert.True(result.HasValue, result.ToString());
        }

        [Fact]
        public void Line_Bare()
        {
            var tokens = tokenizer.Tokenize(@"""foo"" -> ""bar""");
            var result = Shared.Edges(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.Equal("foo", result.Value.First().Target?.Text);
            Assert.Equal("bar", result.Value.Last().Target?.Text);
        }

        [Fact]
        public void Line_Untyped()
        {
            var tokens = tokenizer.Tokenize(@"line(x) foo--bar");
            var result = Untyped.Line(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.Equal("line", result.Value.Classes[0].Value.Name.Text);
            Assert.Single(result.Value.Classes[0].Value.Arguments);
        }

        [Fact]
        public void Line_Typed()
        {
            var tokens = tokenizer.Tokenize(@"line ""foo"" -> ""bar""");
            var result = Typed.Line(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.Equal("line", result.Value.Classes[0].Text);
        }

        [Fact]
        public void Line_Typed_WithAttributes()
        {
            var tokens = tokenizer.Tokenize(@"line ""foo"" -> ""bar"" [stroke-colour=#000000]");
            var result = Typed.Line(tokens);

            Assert.True(result.HasValue, result.ToString());
            AssertEx.Sequence(result.Value.Attributes, new AST.LineStrokeColourAttribute(new Colour(0, 0, 0)));
        }

        [Fact]
        public void Scope_Empty()
        {
            var tokens = tokenizer.Tokenize(@"{}");
            var result = Shared.Scope(Typed.ObjectContent)(tokens);

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
            var result = Shared.Scope(Typed.ObjectContent)(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.Equal(3, result.Value.Length);
        }

        [Fact]
        public void Scope_InContext()
        {
            var tokens = tokenizer.Tokenize(@"object {
    shape=rect
    class foo
	object bar
    object baz
    line bar <- baz
}");
            var result = Typed.Object(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.Equal(5, result.Value.Children.Length);
        }

        [Fact]
        public void Document_SingleNode()
        {
            var tokens = tokenizer.Tokenize(@"object ""foo""");
            var result = Typed.Document(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.Single(result.Value.Declarations);
            Assert.True(result.Value.Declarations.Single().IsT2);
        }

        [Fact]
        public void Document_MultiNode()
        {
            var tokens = tokenizer.Tokenize(@"object ""foo""
object ""bar""
object ""baz""");

            var result = Typed.Document(tokens);

            Assert.True(result.HasValue, result.ToString());
            AssertEx.Sequence(result.Value.Declarations.Where(d => d.IsT2).Select(n => n.AsT2.Name?.Text), "foo", "bar", "baz");
        }

        [Fact]
        public void Document_EmptyLines()
        {
            var tokens = tokenizer.Tokenize(@"object ""foo""

object ""bar""");

            var result = Typed.Document(tokens);

            Assert.True(result.HasValue, result.ToString());
            AssertEx.Sequence(result.Value.Declarations.Where(d => d.IsT2).Select(n => n.AsT2.Name?.Text), "foo", "bar");
        }

        [Fact]
        public void Document_SingleSeparatedLine()
        {
            var tokens = tokenizer.Tokenize(@"object ""foo""; object ""bar""");

            var result = Typed.Document(tokens);

            Assert.True(result.HasValue, result.ToString());
            AssertEx.Sequence(result.Value.Declarations.Where(d => d.IsT2).Select(n => n.AsT2.Name?.Text), "foo", "bar");
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
                d => Assert.True(d.IsT2),
                d => Assert.True(d.IsT2),
                d => Assert.True(d.IsT3)
            );
        }

        [Fact]
        public void Declaration_Attribute()
        {
            var tokens = tokenizer.Tokenize(@"fill=black");
            var result = Typed.DocumentContent(tokens);

            Assert.True(result.HasValue);
            Assert.True(result.Value.IsT0);
        }

        [Fact]
        public void Declaration_Class()
        {
            var tokens = tokenizer.Tokenize(@"class foo [stroke=none]");
            var result = Typed.DocumentContent(tokens);

            Assert.True(result.HasValue);
            Assert.True(result.Value.IsT1);
        }

        [Fact]
        public void Declaration_Object()
        {
            var tokens = tokenizer.Tokenize(@"object foo [shape=square]");
            var result = Typed.DocumentContent(tokens);

            Assert.True(result.HasValue);
            Assert.True(result.Value.IsT2);
        }

        [Fact]
        public void Declaration_AnonymousObject()
        {
            var tokens = tokenizer.Tokenize(@"object {}");
            var result = Typed.DocumentContent(tokens);

            Assert.True(result.HasValue);
            Assert.True(result.Value.IsT2);
        }

        [Fact]
        public void Declaration_WhollyAnonymousObject()
        {
            var tokens = tokenizer.Tokenize(@"object");
            var result = Typed.DocumentContent(tokens);

            Assert.True(result.HasValue);
            Assert.True(result.Value.IsT2);
        }

        [Fact]
        public void Declaration_Line()
        {
            var tokens = tokenizer.Tokenize(@"line foo->bar [stroke=none]");
            var result = Typed.DocumentContent(tokens);

            Assert.True(result.HasValue);
            Assert.True(result.Value.IsT3);
        }

        [Fact]
        public void Declaration_All()
        {
            var tokens = tokenizer.Tokenize(@"fill=black
class foo [stroke=none]
object foo [shape=square]
line foo->bar [offset=1 1]");
            var result = Typed.Document(tokens);

            Assert.True(result.HasValue);
            Assert.Equal(4, result.Value.Declarations.Length);
            Assert.True(result.Value.Declarations[0].IsT0);
            Assert.True(result.Value.Declarations[1].IsT1);
            Assert.True(result.Value.Declarations[2].IsT2);
            Assert.True(result.Value.Declarations[3].IsT3);
        }

        [Fact]
        public void Declaration_BlankLines()
        {
            var tokens = tokenizer.Tokenize(@"fill=black


class foo [stroke=none]");
            var result = Typed.Document(tokens);

            Assert.True(result.HasValue);
            Assert.Equal(2, result.Value.Declarations.Length);
            Assert.True(result.Value.Declarations[0].IsT0);
            Assert.True(result.Value.Declarations[1].IsT1);
        }
    }
}
