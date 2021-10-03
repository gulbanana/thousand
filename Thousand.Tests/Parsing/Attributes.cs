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
            var result = Value.Colour(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.Equal(Colour.White, result.Value);
        }

        [Fact]
        public void ValidColour_Short()
        {
            var tokens = tokenizer.Tokenize(@"#fff");
            var result = Value.Colour(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.Equal(Colour.White, result.Value);
        }

        [Fact]
        public void InvalidColour()
        {
            var tokens = tokenizer.Tokenize(@"#ffff");
            var result = Value.Colour(tokens);

            Assert.False(result.HasValue);
        }

        [Theory]
        [InlineData(1)]
        public void CountingNumber_Valid(decimal v)
        {
            var tokens = tokenizer.Tokenize(v.ToString());
            var result = Value.CountingNumber(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.Equal(v, result.Value);
        }

        [Theory]        
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(0.1)]
        public void CountingNumber_Invalid(decimal v)
        {
            var tokens = tokenizer.Tokenize(v.ToString());
            var result = Value.CountingNumber(tokens);

            Assert.False(result.HasValue, result.ToString());
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        public void WholeNumber_Valid(decimal v)
        {
            var tokens = tokenizer.Tokenize(v.ToString());
            var result = Value.WholeNumber(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.Equal(v, result.Value);
        }

        [Theory]
        [InlineData(0.1)]
        [InlineData(-1)]
        public void WholeNumber_Invalid(decimal v)
        {
            var tokens = tokenizer.Tokenize(v.ToString());
            var result = Value.WholeNumber(tokens);

            Assert.False(result.HasValue, result.ToString());
        }

        [Theory]
        [InlineData(1)]
        [InlineData(0)]        
        [InlineData(-1)]
        public void Integer_Valid(decimal v)
        {
            var tokens = tokenizer.Tokenize(v.ToString());
            var result = Value.Integer(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.Equal(v, result.Value);
        }

        [Theory]
        [InlineData(0.1)]
        public void Integer_Invalid(decimal v)
        {
            var tokens = tokenizer.Tokenize(v.ToString());
            var result = Value.Integer(tokens);

            Assert.False(result.HasValue, result.ToString());
        }

        [Theory]
        [InlineData(1)]
        [InlineData(0.1)]
        [InlineData(0)]
        [InlineData(-1)]
        public void Decimal_Valid(decimal v)
        {
            var tokens = tokenizer.Tokenize(v.ToString());
            var result = Value.Decimal(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.Equal(v, result.Value);
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
            Assert.IsType<AST.LineStrokeAttribute>(result.Value);

            var lsa = (AST.LineStrokeAttribute)result.Value;

            Assert.NotNull(lsa.Colour);
            Assert.Equal(Colour.Black, lsa.Colour);
        }

        [Fact]
        public void StrokeShorthand_SingleWidth()
        {
            var tokens = tokenizer.Tokenize($"stroke=none");
            var result = AttributeParsers.LineStrokeAttribute(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.IsType<AST.LineStrokeAttribute>(result.Value);

            var lsa = (AST.LineStrokeAttribute)result.Value;

            Assert.NotNull(lsa.Width);
            Assert.True(lsa.Width is ZeroWidth);
        }

        [Fact]
        public void StrokeShorthand_SingleStyle()
        {
            var tokens = tokenizer.Tokenize($"stroke=dashed");
            var result = AttributeParsers.LineStrokeAttribute(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.IsType<AST.LineStrokeAttribute>(result.Value);

            var lsa = (AST.LineStrokeAttribute)result.Value;

            Assert.NotNull(lsa.Style);
            Assert.Equal(StrokeKind.Dashed, lsa.Style);
        }

        [Fact]
        public void StrokeShorthand_Multiple()
        {
            var tokens = tokenizer.Tokenize($"stroke=2 black");
            var result = AttributeParsers.LineStrokeAttribute(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.IsType<AST.LineStrokeAttribute>(result.Value);

            var lsa = (AST.LineStrokeAttribute)result.Value;

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
            Assert.IsType<AST.LineStrokeAttribute>(result.Value);

            var lsa = (AST.LineStrokeAttribute)result.Value;

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

            var lsa = (AST.LineStrokeAttribute)result.Value.Attributes.Single().AsT2;
            
            Assert.NotNull(lsa.Colour);
            Assert.Null(lsa.Width);
            Assert.NotNull(lsa.Style);
        }

        [Fact]
        public void EnumAlias()
        {
            var tokens1 = tokenizer.Tokenize(@"shape=rect");
            var tokens2 = tokenizer.Tokenize(@"shape=rectangle");
            var result1 = AttributeParsers.NodeShapeAttribute(tokens1);
            var result2 = AttributeParsers.NodeShapeAttribute(tokens2);

            Assert.True(result1.HasValue, result1.ToString());
            Assert.True(result2.HasValue, result2.ToString());
            Assert.Equal(result1.Value, result2.Value);
        }
    }
}
