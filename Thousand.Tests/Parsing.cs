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
            var tokens = tokenizer.Tokenize(@"[a=b]");
            var result = Parsers.AttributeList(tokens);

            Assert.True(result.HasValue, result.ToString());
            AssertEx.Sequence(result.Value, new AST.Attribute("a", "b"));
        }

        [Fact]
        public void ValidAttributeList_Multiple()
        {
            var tokens = tokenizer.Tokenize(@"[a=b,c=d,e=f]");
            var result = Parsers.AttributeList(tokens);

            Assert.True(result.HasValue, result.ToString());
            AssertEx.Sequence(result.Value, new AST.Attribute("a", "b"), new AST.Attribute("c", "d"), new AST.Attribute("e", "f"));
        }

        [Fact]
        public void ValidAttributeList_Whitespace()
        {
            var tokens = tokenizer.Tokenize(@"[ a=b,c = d, e=f]");
            var result = Parsers.AttributeList(tokens);

            Assert.True(result.HasValue, result.ToString());
            AssertEx.Sequence(result.Value, new AST.Attribute("a", "b"), new AST.Attribute("c", "d"), new AST.Attribute("e", "f"));
        }

        [Fact]
        public void ValidNode()
        {
            var tokens = tokenizer.Tokenize(@"node ""foo""");
            var result = Parsers.Node(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.Equal(new AST.Node("foo", Array.Empty<AST.Attribute>()), result.Value);
        }

        [Fact]
        public void ValidNode_WhiteSpace()
        {
            var tokens = tokenizer.Tokenize(@"   node     ""foo""    ");
            var result = Parsers.Node(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.Equal(new AST.Node("foo", Array.Empty<AST.Attribute>()), result.Value);
        }

        [Fact]
        public void ValidNode_Multiline()
        {
            var tokens = tokenizer.Tokenize(@"node ""foo
bar""");
            var result = Parsers.Node(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.Equal(new AST.Node("foo"+Environment.NewLine+"bar", Array.Empty<AST.Attribute>()), result.Value);
        }

        [Fact]
        public void InvalidNode_WrongKeyword()
        {
            var tokens = tokenizer.Tokenize(@"nod ""foo""");
            var result = Parsers.Node(tokens);

            Assert.False(result.HasValue, result.ToString());
        }

        [Fact]
        public void InvalidNode_NoLabel()
        {
            var tokens = tokenizer.Tokenize(@"node");
            var result = Parsers.Node(tokens);

            Assert.False(result.HasValue, result.ToString());
        }

        [Fact]
        public void ValidDocument_SingleNode()
        {
            var tokens = tokenizer.Tokenize(@"node ""foo""");
            var result = Parsers.Document(tokens);

            Assert.True(result.HasValue, result.ToString());
            AssertEx.Sequence(result.Value.Nodes, new AST.Node("foo", Array.Empty<AST.Attribute>()) );
        }

        [Fact]
        public void ValidDocument_MultiNode()
        {
            var tokens = tokenizer.Tokenize(@"node ""foo""
node ""bar""
node ""baz""");

            var result = Parsers.Document(tokens);

            Assert.True(result.HasValue, result.ToString());
            AssertEx.Sequence(result.Value.Nodes, new AST.Node("foo", Array.Empty<AST.Attribute>()), new AST.Node("bar", Array.Empty<AST.Attribute>()), new AST.Node("baz", Array.Empty<AST.Attribute>()));
        }

        [Fact]
        public void ValidDocument_EmptyLines()
        {
            var tokens = tokenizer.Tokenize(@"node ""foo""

node ""bar""");

            var result = Parsers.Document(tokens);

            Assert.True(result.HasValue, result.ToString());
            AssertEx.Sequence(result.Value.Nodes, new AST.Node("foo", Array.Empty<AST.Attribute>()), new AST.Node("bar", Array.Empty<AST.Attribute>()));
        }
    }
}
