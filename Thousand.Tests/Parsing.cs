using System;
using System.Linq;
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
        public void ValidAttributeList_Single()
        {
            var tokens = tokenizer.Tokenize(@"[shape=square]");
            var result = Parser.AttributeList(tokens);

            Assert.True(result.HasValue, result.ToString());
            AssertEx.Sequence(result.Value, new AST.NodeShapeAttribute(ShapeKind.Square));
        }

        [Fact]
        public void ValidAttributeList_Multiple()
        {
            var tokens = tokenizer.Tokenize(@"[shape=square,shape=oval,shape=rounded]");
            var result = Parser.AttributeList(tokens);

            Assert.True(result.HasValue, result.ToString());
            AssertEx.Sequence(result.Value, new AST.NodeShapeAttribute(ShapeKind.Square), new AST.NodeShapeAttribute(ShapeKind.Oval), new AST.NodeShapeAttribute(ShapeKind.Rounded));
        }

        [Fact]
        public void ValidAttributeList_Whitespace()
        {
            var tokens = tokenizer.Tokenize(@"[ shape=square,shape = square, shape=square]");
            var result = Parser.AttributeList(tokens);

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
            AssertEx.Sequence(result.Value.Nodes, new AST.Node("foo", Array.Empty<AST.NodeAttribute>()) );
        }

        [Fact]
        public void ValidDocument_MultiNode()
        {
            var tokens = tokenizer.Tokenize(@"node ""foo""
node ""bar""
node ""baz""");

            var result = Parser.Document(tokens);

            Assert.True(result.HasValue, result.ToString());
            AssertEx.Sequence(result.Value.Nodes, new AST.Node("foo", Array.Empty<AST.NodeAttribute>()), new AST.Node("bar", Array.Empty<AST.NodeAttribute>()), new AST.Node("baz", Array.Empty<AST.NodeAttribute>()));
        }

        [Fact]
        public void ValidDocument_EmptyLines()
        {
            var tokens = tokenizer.Tokenize(@"node ""foo""

node ""bar""");

            var result = Parser.Document(tokens);

            Assert.True(result.HasValue, result.ToString());
            AssertEx.Sequence(result.Value.Nodes, new AST.Node("foo", Array.Empty<AST.NodeAttribute>()), new AST.Node("bar", Array.Empty<AST.NodeAttribute>()));
        }
    }
}
