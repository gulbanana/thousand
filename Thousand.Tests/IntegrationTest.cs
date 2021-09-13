using System.Linq;
using Xunit;

namespace Thousand.Tests
{
    public class IntegrationTest
    {
        [Fact]
        public void SourceToTokens1()
        {
            var input = @"node ""Foo""";

            var output = Tokenizers.Thousand.Tokenize(input);

            AssertEx.Sequence(output.Select(t => t.Kind), Token.Keyword, Token.String);
            AssertEx.Sequence(output.Select(t => t.ToStringValue()), "node", @"""Foo""");
        }

        [Fact]
        public void SourceToTokens2()
        {
            var input = @"foo
bar";

            var output = Tokenizers.Thousand.Tokenize(input);

            AssertEx.Sequence(output.Select(t => t.Kind), Token.Keyword, Token.NewLine, Token.Keyword);
        }
    }
}
