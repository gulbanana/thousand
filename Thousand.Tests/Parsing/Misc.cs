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
        public void ValidAttributeList_Single()
        {
            var tokens = tokenizer.Tokenize(@"[shape=square]");
            var result = Parser.AttributeList(AttributeParsers.NodeAttribute)(tokens);

            Assert.True(result.HasValue, result.ToString());
            AssertEx.Sequence(result.Value, new AST.NodeShapeAttribute(ShapeKind.Square));
        }

        [Fact]
        public void ValidAttributeList_Multiple()
        {
            var tokens = tokenizer.Tokenize(@"[shape=square,shape=oval]");
            var result = Parser.AttributeList(AttributeParsers.NodeAttribute)(tokens);

            Assert.True(result.HasValue, result.ToString());
            AssertEx.Sequence(result.Value, new AST.NodeShapeAttribute(ShapeKind.Square), new AST.NodeShapeAttribute(ShapeKind.Oval));
        }

        [Fact]
        public void ValidAttributeList_Whitespace()
        {
            var tokens = tokenizer.Tokenize(@"[ shape=square,shape = square, shape=square]");
            var result = Parser.AttributeList(AttributeParsers.NodeAttribute)(tokens);

            Assert.True(result.HasValue, result.ToString());
            AssertEx.Sequence(result.Value, new AST.NodeShapeAttribute(ShapeKind.Square), new AST.NodeShapeAttribute(ShapeKind.Square), new AST.NodeShapeAttribute(ShapeKind.Square));
        }

        [Fact]
        public void ValidNode()
        {
            var tokens = tokenizer.Tokenize(@"object foo");
            var result = Parser.Object(tokens);

            Assert.True(result.HasValue, result.ToString());
            AssertEx.Sequence(result.Value.Classes, "object");
            Assert.Equal("foo", result.Value.Name);
        }

        [Fact]
        public void ValidNode_WhiteSpace()
        {
            var tokens = tokenizer.Tokenize(@"   object     foo    ");
            var result = Parser.Object(tokens);

            Assert.True(result.HasValue, result.ToString());
            AssertEx.Sequence(result.Value.Classes, "object");
            Assert.Equal("foo", result.Value.Name);
        }

        [Fact]
        public void ValidNode_Multiline()
        {
            var tokens = tokenizer.Tokenize(@"object ""foo
bar""");
            var result = Parser.Object(tokens);

            Assert.True(result.HasValue, result.ToString());
            AssertEx.Sequence(result.Value.Classes, "object");
            Assert.Equal("foo" + Environment.NewLine + "bar", result.Value.Name);
        }

        [Fact]
        public void ValidNode_Attributed()
        {
            var tokens = tokenizer.Tokenize(@"object ""foo"" [label=""bar""]");
            var result = Parser.Object(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.Equal("foo", result.Value.Name);
            AssertEx.Sequence(result.Value.Attributes, new AST.TextLabelAttribute("bar"));
        }

        [Fact]
        public void ValidEdges()
        {
            var tokens = tokenizer.Tokenize(@"""foo"" -> ""bar""");
            var result = Parser.Edges(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.Equal("foo", result.Value.First().Target);
            Assert.Equal("bar", result.Value.Last().Target);
        }

        [Fact]
        public void ValidEdges_Attributed()
        {
            var tokens = tokenizer.Tokenize(@"line ""foo"" -> ""bar"" [strokeColour=#000000]");
            var result = Parser.Line(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.Equal("foo", result.Value.Elements.First().Target);
            Assert.Equal("bar", result.Value.Elements.Last().Target);
            AssertEx.Sequence(result.Value.Attributes, new AST.StrokeColourAttribute(new Colour(0, 0, 0)));
        }

        [Fact]
        public void ValidNode_CustomClass()
        {
            var tokens = tokenizer.Tokenize(@"foo bar");
            var result = Parser.Object(tokens);

            Assert.True(result.HasValue, result.ToString());
            AssertEx.Sequence(result.Value.Classes, "foo");
            Assert.Equal("bar", result.Value.Name);
        }

        [Fact]
        public void ValidNode_CustomClasses()
        {
            var tokens = tokenizer.Tokenize(@"foo.bar baz");
            var result = Parser.Object(tokens);

            Assert.True(result.HasValue, result.ToString());
            AssertEx.Sequence(result.Value.Classes, "foo", "bar");
            Assert.Equal("baz", result.Value.Name);
        }

        [Fact]
        public void ValidNode_Anonymous()
        {
            var tokens = tokenizer.Tokenize(@"object");
            var result = Parser.Object(tokens);

            AssertEx.Sequence(result.Value.Classes, "object");
            Assert.True(result.HasValue, result.ToString());
        }

        [Fact]
        public void ValidDocument_SingleNode()
        {
            var tokens = tokenizer.Tokenize(@"object ""foo""");
            var result = Parser.Document(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.Single(result.Value.Declarations);
            Assert.True(result.Value.Declarations.Single().IsT2);
        }

        [Fact]
        public void ValidDocument_MultiNode()
        {
            var tokens = tokenizer.Tokenize(@"object ""foo""
object ""bar""
object ""baz""");

            var result = Parser.Document(tokens);

            Assert.True(result.HasValue, result.ToString());
            AssertEx.Sequence(result.Value.Declarations.Where(d => d.IsT2).Select(n => n.AsT2.Name), "foo", "bar", "baz");
        }

        [Fact]
        public void ValidDocument_EmptyLines()
        {
            var tokens = tokenizer.Tokenize(@"object ""foo""

object ""bar""");

            var result = Parser.Document(tokens);

            Assert.True(result.HasValue, result.ToString());
            AssertEx.Sequence(result.Value.Declarations.Where(d => d.IsT2).Select(n => n.AsT2.Name), "foo", "bar");
        }

        [Fact]
        public void ValidDocument_NodesAndEdge()
        {
            var tokens = tokenizer.Tokenize(@"object foo
object bar
line foo -> bar");
            var result = Parser.Document(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.Collection(result.Value.Declarations,
                d => Assert.True(d.IsT2),
                d => Assert.True(d.IsT2),
                d =>
                {
                    Assert.True(d.IsT3);
                    AssertEx.Sequence(d.AsT3.Elements, new AST.Edge("foo", ArrowKind.Forward), new AST.Edge("bar", null));
                });
        }

        [Fact]
        public void ValidDeclaration_Attribute()
        {
            var tokens = tokenizer.Tokenize(@"fill=black");
            var result = Parser.DocumentDeclaration(tokens);

            Assert.True(result.HasValue);
            Assert.True(result.Value.IsT0);
        }

        [Fact]
        public void ValidDeclaration_Class()
        {
            var tokens = tokenizer.Tokenize(@"class foo [stroke=none]");
            var result = Parser.DocumentDeclaration(tokens);

            Assert.True(result.HasValue);
            Assert.True(result.Value.IsT1);
        }

        [Fact]
        public void ValidDeclaration_Object()
        {
            var tokens = tokenizer.Tokenize(@"object foo [shape=square]");
            var result = Parser.DocumentDeclaration(tokens);

            Assert.True(result.HasValue);
            Assert.True(result.Value.IsT2);
        }

        [Fact]
        public void ValidDeclaration_Line()
        {
            var tokens = tokenizer.Tokenize(@"line foo->bar [offset=(0,0)]");
            var result = Parser.DocumentDeclaration(tokens);

            Assert.True(result.HasValue);
            Assert.True(result.Value.IsT3);
        }
    }
}
