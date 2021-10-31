using System;
using System.Linq;
using Thousand.Parse;
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
        public void Keywords()
        {
            var input = @"node edge foo bar";

            var output = sut.Tokenize(input);

            AssertEx.Sequence(output.Select(t => t.Kind), TokenKind.Identifier, TokenKind.Identifier, TokenKind.Identifier, TokenKind.Identifier);
            AssertEx.Sequence(output.Select(t => t.ToStringValue()), "node", "edge", "foo", "bar");
        }

        [Theory]
        [InlineData("#000000")]
        [InlineData("#123456")]
        [InlineData("#ffffff")]
        [InlineData("#ccc")]
        public void Colour(string input)
        {
            var output = sut.Tokenize(input);

            AssertEx.Sequence(output.Select(t => t.Kind), TokenKind.Colour);
            AssertEx.Sequence(output.Select(t => t.ToStringValue()), input);
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

            AssertEx.Sequence(output.Select(t => t.Kind), TokenKind.Identifier, TokenKind.LineSeparator, TokenKind.Identifier);
        }

        [Fact]
        public void MultilineString()
        {
            var input = @" 
""foo
bar"" ""baz""
";

            var output = sut.Tokenize(input);

            AssertEx.Sequence(output.Select(t => t.Kind), TokenKind.LineSeparator, TokenKind.String, TokenKind.String, TokenKind.LineSeparator);
            Assert.Equal(@"""foo
bar""", output.ElementAt(1).ToStringValue());
        }

        [Fact]
        public void SubclassWithoutBase()
        {
            var output = sut.Tokenize(@"class foo : [label=""bar""]");

            AssertEx.Sequence(output.Select(t => t.Kind), TokenKind.ClassKeyword, TokenKind.Identifier, TokenKind.Colon, TokenKind.LeftBracket, TokenKind.Identifier, TokenKind.EqualsSign, TokenKind.String, TokenKind.RightBracket);
        }

        [Theory]
        [InlineData("f")]
        [InlineData("f2")]
        [InlineData("f-b")]
        [InlineData("foo")]
        [InlineData("FOO")]
        [InlineData("foo-bar")]
        [InlineData("FOO-BAR2-BAZ")]
        [InlineData("foo2")]
        public void Identifier(string input)
        {
            var output = sut.Tokenize(input);

            AssertEx.Sequence(output.Select(t => t.Kind), TokenKind.Identifier);
        }

        [Theory]
        [InlineData("2")]
        [InlineData("f-")]
        [InlineData("f--b")]
        [InlineData("foo-")]        
        public void NotIdentifier(string input)
        {
            try
            {
                var output = sut.Tokenize(input);
                Assert.True(output.Count() != 1 || output.Single().Kind != TokenKind.Identifier);
            }
            catch (Exception) 
            { 
                // we have successfully failed
            }
        }

        [Fact]
        public void IdentifiersAndArrows()
        {
            var output = sut.Tokenize(@"foo -- foo-bar <- f-> b-f b--f b -- f -1f");

            AssertEx.Sequence(output.Select(t => t.Kind), 
                TokenKind.Identifier, // foo
                TokenKind.NoArrow,    // --
                TokenKind.Identifier, // foo-bar
                TokenKind.LeftArrow,  // <-
                TokenKind.Identifier, // f
                TokenKind.RightArrow, // ->
                TokenKind.Identifier, // b-f
                TokenKind.Identifier, // b--f
                TokenKind.NoArrow,    // b--f
                TokenKind.Identifier, // b--f
                TokenKind.Identifier, // b
                TokenKind.NoArrow,    // --
                TokenKind.Identifier, // f
                TokenKind.Number,     // -1
                TokenKind.Identifier  // f
            );
        }
    }
}
