using System;
using Thousand.Model;
using Thousand.Parse;
using Xunit;

namespace Thousand.Tests
{
    public class Parsing
    {
        private readonly Superpower.Tokenizer<TokenKind> tokenizer;

        public Parsing()
        {
            tokenizer = Tokenizer.Build();            
        }

        [Fact]
        public void ValidColour()
        {
            var tokens = tokenizer.Tokenize(@"#ffffff");
            var result = AttributeParsers.ColourValue(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.Equal(Colour.White, result.Value);
        }

        [Fact]
        public void ValidColour_Short()
        {
            var tokens = tokenizer.Tokenize(@"#fff");
            var result = AttributeParsers.ColourValue(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.Equal(Colour.White, result.Value);
        }

        [Fact]
        public void InvalidColour()
        {
            var tokens = tokenizer.Tokenize(@"#ffff");
            var result = AttributeParsers.ColourValue(tokens);

            Assert.False(result.HasValue);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(100)]
        [InlineData(10000)]
        public void ValidInteger(int v)
        {
            var tokens = tokenizer.Tokenize(v.ToString());
            var result = AttributeParsers.CountingNumberValue(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.Equal(v, result.Value);
        }

        [Theory]
        [InlineData("0")]
        [InlineData("-1")]
        public void InvalidInteger(string v)
        {
            var tokens = tokenizer.Tokenize(v);
            var result = AttributeParsers.CountingNumberValue(tokens);

            Assert.False(result.HasValue, result.ToString());
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
            var tokens = tokenizer.Tokenize(@"[shape=square,shape=oval,shape=rounded]");
            var result = Parser.AttributeList(AttributeParsers.NodeAttribute)(tokens);

            Assert.True(result.HasValue, result.ToString());
            AssertEx.Sequence(result.Value, new AST.NodeShapeAttribute(ShapeKind.Square), new AST.NodeShapeAttribute(ShapeKind.Oval), new AST.NodeShapeAttribute(ShapeKind.Rounded));
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
            var tokens = tokenizer.Tokenize(@"node ""foo""");
            var result = Parser.Node(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.Equal(new AST.Node("foo", Array.Empty<AST.NodeAttribute>()), result.Value);
        }

        [Fact]
        public void ValidNode_WhiteSpace()
        {
            var tokens = tokenizer.Tokenize(@"   node     ""foo""    ");
            var result = Parser.Node(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.Equal(new AST.Node("foo", Array.Empty<AST.NodeAttribute>()), result.Value);
        }

        [Fact]
        public void ValidNode_Multiline()
        {
            var tokens = tokenizer.Tokenize(@"node ""foo
bar""");
            var result = Parser.Node(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.Equal(new AST.Node("foo"+Environment.NewLine+"bar", Array.Empty<AST.NodeAttribute>()), result.Value);
        }

        [Fact]
        public void ValidNode_Attributed()
        {
            var tokens = tokenizer.Tokenize(@"node ""foo"" [label=""bar""]");
            var result = Parser.Node(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.Equal("foo", result.Value.Label);
            AssertEx.Sequence(result.Value.Attributes, new AST.NodeLabelAttribute("bar"));
        }

        [Fact]
        public void ValidEdge()
        {
            var tokens = tokenizer.Tokenize(@"edge ""foo"" ""bar""");
            var result = Parser.Edge(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.Equal("foo", result.Value.From);
            Assert.Equal("bar", result.Value.To);
            Assert.Empty(result.Value.Attributes);
        }

        [Fact]
        public void ValidEdge_Attributed()
        {
            var tokens = tokenizer.Tokenize(@"edge ""foo"" ""bar"" [stroke=#000000]");
            var result = Parser.Edge(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.Equal("foo", result.Value.From);
            Assert.Equal("bar", result.Value.To);
            AssertEx.Sequence(result.Value.Attributes, new AST.EdgeStrokeAttribute(new Colour(0, 0, 0)));
        }

        [Fact]
        public void InvalidNode_WrongKeyword()
        {
            var tokens = tokenizer.Tokenize(@"nod ""foo""");
            var result = Parser.Node(tokens);

            Assert.False(result.HasValue, result.ToString());
        }

        [Fact]
        public void InvalidNode_NoLabel()
        {
            var tokens = tokenizer.Tokenize(@"node");
            var result = Parser.Node(tokens);

            Assert.False(result.HasValue, result.ToString());
        }

        [Fact]
        public void ValidDocument_SingleNode()
        {
            var tokens = tokenizer.Tokenize(@"node ""foo""");
            var result = Parser.Document(tokens);

            Assert.True(result.HasValue, result.ToString());
            AssertEx.Sequence(result.Value.Declarations, new AST.Node("foo", Array.Empty<AST.NodeAttribute>()) );
        }

        [Fact]
        public void ValidDocument_MultiNode()
        {
            var tokens = tokenizer.Tokenize(@"node ""foo""
node ""bar""
node ""baz""");

            var result = Parser.Document(tokens);

            Assert.True(result.HasValue, result.ToString());
            AssertEx.Sequence(result.Value.Declarations, 
                new AST.Node("foo", Array.Empty<AST.NodeAttribute>()), 
                new AST.Node("bar", Array.Empty<AST.NodeAttribute>()), 
                new AST.Node("baz", Array.Empty<AST.NodeAttribute>()));
        }

        [Fact]
        public void ValidDocument_EmptyLines()
        {
            var tokens = tokenizer.Tokenize(@"node ""foo""

node ""bar""");

            var result = Parser.Document(tokens);

            Assert.True(result.HasValue, result.ToString());
            AssertEx.Sequence(result.Value.Declarations, new AST.Node("foo", 
                Array.Empty<AST.NodeAttribute>()), 
                new AST.Node("bar", Array.Empty<AST.NodeAttribute>()));
        }

        [Fact]
        public void ValidDocument_NodesAndEdge()
        {
            var tokens = tokenizer.Tokenize(@"node ""foo""
node ""bar""
edge ""foo"" ""bar""");
            var result = Parser.Document(tokens);

            Assert.True(result.HasValue, result.ToString());
            AssertEx.Sequence(result.Value.Declarations, 
                new AST.Node("foo", Array.Empty<AST.NodeAttribute>()),
                new AST.Node("bar", Array.Empty<AST.NodeAttribute>()),
                new AST.Edge("foo", "bar", Array.Empty<AST.EdgeAttribute>()));
        }
    }
}
