using System.Linq;
using Xunit;

namespace Thousand.Tests
{
    public class Tokenization
    {
        private readonly Superpower.Tokenizer<TokenKind> sut;

        public Tokenization()
        {
            sut = Tokenizer.Build();
        }

        [Fact]
        public void BasicElements()
        {
            var input = @"node ""Foo""";

            var output = sut.Tokenize(input);

            AssertEx.Sequence(output.Select(t => t.Kind), TokenKind.Keyword, TokenKind.String);
            AssertEx.Sequence(output.Select(t => t.ToStringValue()), "node", @"""Foo""");
        }

        [Theory]
        [InlineData("foo\nbar")]
        [InlineData("foo \nbar")]
        [InlineData("foo\n bar")]
        [InlineData("foo \n bar")]
        [InlineData("foo\r\nbar")]
        [InlineData("foo \r\nbar")]
        [InlineData("foo\r\n bar")]
        [InlineData("foo \r\n bar")]
        public void NewlineVariants(string input)
        {
            var output = sut.Tokenize(input);

            AssertEx.Sequence(output.Select(t => t.Kind), TokenKind.Keyword, TokenKind.NewLine, TokenKind.Keyword);
        }

        [Fact]
        public void MultilineString()
        {
            var input = @" 
""foo
bar"" ""baz""
";

            var output = sut.Tokenize(input);

            AssertEx.Sequence(output.Select(t => t.Kind), TokenKind.NewLine, TokenKind.String, TokenKind.String, TokenKind.NewLine);
            Assert.Equal(@"""foo
bar""", output.ElementAt(1).ToStringValue());
        }
    }
}
