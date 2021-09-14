using System.Linq;
using Xunit;

namespace Thousand.Tests
{
    public class Tokenization
    {
        private readonly Superpower.Tokenizer<Kind> sut;

        public Tokenization()
        {
            sut = Tokenizer.Build();
        }

        [Fact]
        public void BasicElements()
        {
            var input = @"node ""Foo""";

            var output = sut.Tokenize(input);

            AssertEx.Sequence(output.Select(t => t.Kind), Kind.Keyword, Kind.String);
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

            AssertEx.Sequence(output.Select(t => t.Kind), Kind.Keyword, Kind.NewLine, Kind.Keyword);
        }

        [Fact]
        public void MultilineString()
        {
            var input = @" 
""foo
bar"" ""baz""
";

            var output = sut.Tokenize(input);

            AssertEx.Sequence(output.Select(t => t.Kind), Kind.NewLine, Kind.String, Kind.String, Kind.NewLine);
            Assert.Equal(@"""foo
bar""", output.ElementAt(1).ToStringValue());
        }
    }
}
