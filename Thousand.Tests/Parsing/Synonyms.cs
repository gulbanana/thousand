using Thousand.Parse;
using Xunit;

namespace Thousand.Tests.Parsing
{
    public class Synonyms
    {
        private readonly Superpower.Tokenizer<TokenKind> tokenizer;

        public Synonyms()
        {
            tokenizer = Tokenizer.Build();            
        }

        [Fact]
        public void Key()
        {
            var tokens1 = tokenizer.Tokenize(@"col=1");
            var tokens2 = tokenizer.Tokenize(@"column=1");
            var result1 = Typed.NodeAttribute(tokens1);
            var result2 = Typed.NodeAttribute(tokens2);

            Assert.True(result1.HasValue, result1.ToString());
            Assert.True(result2.HasValue, result2.ToString());
            Assert.Equal(result1.Value, result2.Value);
        }

        [Fact]
        public void Value()
        {
            var tokens1 = tokenizer.Tokenize(@"shape=rect");
            var tokens2 = tokenizer.Tokenize(@"shape=rectangle");
            var result1 = Typed.NodeAttribute(tokens1);
            var result2 = Typed.NodeAttribute(tokens2);

            Assert.True(result1.HasValue, result1.ToString());
            Assert.True(result2.HasValue, result2.ToString());
            Assert.Equal(result1.Value, result2.Value);
        }
    }
}
