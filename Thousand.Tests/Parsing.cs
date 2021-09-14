using System;
using System.Linq;
using Xunit;

namespace Thousand.Tests
{
    public class Parsing
    {
        private readonly Superpower.Tokenizer<Kind> tokenizer;

        public Parsing()
        {
            tokenizer = Tokenizer.Build();            
        }

        [Fact]
        public void ValidNode()
        {
            var tokens = tokenizer.Tokenize(@"node ""foo""");
            var result = Parsers.Node(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.Equal(new AST.Node("foo"), result.Value);
        }

        [Fact]
        public void ValidNode_WhiteSpace()
        {
            var tokens = tokenizer.Tokenize(@"   node     ""foo""    ");
            var result = Parsers.Node(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.Equal(new AST.Node("foo"), result.Value);
        }

        [Fact]
        public void ValidNode_Multiline()
        {
            var tokens = tokenizer.Tokenize(@"node ""foo
bar""");
            var result = Parsers.Node(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.Equal(new AST.Node("foo"+Environment.NewLine+"bar"), result.Value);
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
            AssertEx.Sequence(result.Value.Nodes, new AST.Node("foo") );
        }

        [Fact]
        public void ValidDocument_MultiNode()
        {
            var tokens = tokenizer.Tokenize(@"node ""foo""
node ""bar""
node ""baz""");

            var result = Parsers.Document(tokens);

            Assert.True(result.HasValue, result.ToString());
            AssertEx.Sequence(result.Value.Nodes, new AST.Node("foo"), new AST.Node("bar"), new AST.Node("baz"));
        }

        [Fact]
        public void ValidDocument_EmptyLines()
        {
            var tokens = tokenizer.Tokenize(@"node ""foo""

node ""bar""");

            var result = Parsers.Document(tokens);

            Assert.True(result.HasValue, result.ToString());
            AssertEx.Sequence(result.Value.Nodes, new AST.Node("foo"), new AST.Node("bar"));
        }
    }
}
