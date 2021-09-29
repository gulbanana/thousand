using System.Linq;
using Thousand.Model;
using Thousand.Parse;
using Xunit;

namespace Thousand.Tests.Parsing
{
    public class Attributes
    {
        private readonly Superpower.Tokenizer<TokenKind> tokenizer;

        public Attributes()
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
        public void StrokeShorthand_InvalidKeyword()
        {
            var tokens = tokenizer.Tokenize(@"stroke=square");
            var result = AttributeParsers.LineStrokeAttribute(tokens);

            Assert.False(result.HasValue, result.ToString());
        }

        [Theory, InlineData("#000"), InlineData("black")]
        public void StrokeShorthand_SingleColour(string c)
        {
            var tokens = tokenizer.Tokenize($"stroke={c}");
            var result = AttributeParsers.LineStrokeAttribute(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.IsType<AST.StrokeShorthandAttribute>(result.Value);

            var lsa = (AST.StrokeShorthandAttribute)result.Value;

            Assert.NotNull(lsa.Colour);
            Assert.Equal(Colour.Black, lsa.Colour);
        }

        [Fact]
        public void StrokeShorthand_SingleWidth()
        {
            var tokens = tokenizer.Tokenize($"stroke=none");
            var result = AttributeParsers.LineStrokeAttribute(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.IsType<AST.StrokeShorthandAttribute>(result.Value);

            var lsa = (AST.StrokeShorthandAttribute)result.Value;

            Assert.NotNull(lsa.Width);
            Assert.True(lsa.Width is ZeroWidth);
        }

        [Fact]
        public void StrokeShorthand_Multiple()
        {
            var tokens = tokenizer.Tokenize($"stroke=2 black");
            var result = AttributeParsers.LineStrokeAttribute(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.IsType<AST.StrokeShorthandAttribute>(result.Value);

            var lsa = (AST.StrokeShorthandAttribute)result.Value;

            Assert.NotNull(lsa.Colour);
            Assert.Equal(Colour.Black, lsa.Colour);

            Assert.NotNull(lsa.Width);
            Assert.True(lsa.Width is PositiveWidth(2));

            Assert.Null(lsa.Style);
        }

        [Fact]
        public void StrokeShorthand_All()
        {
            var tokens = tokenizer.Tokenize($"stroke=solid green hairline");
            var result = AttributeParsers.LineStrokeAttribute(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.IsType<AST.StrokeShorthandAttribute>(result.Value);

            var lsa = (AST.StrokeShorthandAttribute)result.Value;

            Assert.NotNull(lsa.Colour);
            Assert.Equal(Colour.Green, lsa.Colour);

            Assert.NotNull(lsa.Width);
            Assert.True(lsa.Width is HairlineWidth);

            Assert.NotNull(lsa.Style);
            Assert.Equal(StrokeKind.Solid, lsa.Style!.Value);
        }

        [Fact]
        public void StrokeShorthand_InContext()
        {
            var tokens = tokenizer.Tokenize($"object [stroke=dashed black]");
            var result = Parser.Object(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.Single(result.Value.Attributes);
            Assert.True(result.Value.Attributes.Single().IsT2);

            var lsa = (AST.StrokeShorthandAttribute)result.Value.Attributes.Single().AsT2;
            
            Assert.NotNull(lsa.Colour);
            Assert.Null(lsa.Width);
            Assert.NotNull(lsa.Style);
        }
    }
}
