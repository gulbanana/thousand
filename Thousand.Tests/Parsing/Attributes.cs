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
            var tokens = tokenizer.Tokenize($"stroke=short-dash");
            var result = AttributeParsers.LineStrokeAttribute(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.IsType<AST.LineStrokeAttribute>(result.Value);

            var lsa = (AST.LineStrokeAttribute)result.Value;

            Assert.NotNull(lsa.Style);
            Assert.Equal(StrokeKind.ShortDash, lsa.Style);
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
            var tokens = tokenizer.Tokenize($"object [stroke=long-dash black]");
            var result = Typed.Object(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.Single(result.Value.Attributes);
            Assert.True(result.Value.Attributes.Single().IsT4);

            var lsa = (AST.LineStrokeAttribute)result.Value.Attributes.Single();
            
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

        [Theory]
        [InlineData("start start", AlignmentKind.Start, AlignmentKind.Start)]   // two explicit tracks
        [InlineData("end", AlignmentKind.End, AlignmentKind.End)]               // two implicit tracks
        [InlineData("left", AlignmentKind.Start, AlignmentKind.Center)]         // one implicit column
        [InlineData("left center", AlignmentKind.Start, AlignmentKind.Center)]  // one explicit column, one explicit track
        [InlineData("bottom", AlignmentKind.Center, AlignmentKind.End)]         // one explicit row
        [InlineData("bottom right", AlignmentKind.End, AlignmentKind.End)]      // one explicit row, one explicit column
        [InlineData("start top", AlignmentKind.Start, AlignmentKind.Start)]     // one explicit track, one explicit row
        public void JustifyShorthand(string value, AlignmentKind expectedHorizontal, AlignmentKind expectedVertical)
        {
            var tokens = tokenizer.Tokenize($"justify={value}");
            var result = AttributeParsers.RegionJustifyAttribute(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.Equal(expectedHorizontal, ((AST.RegionJustifyAttribute)result.Value).Columns);
            Assert.Equal(expectedVertical, ((AST.RegionJustifyAttribute)result.Value).Rows);
        }

        [Theory]
        [InlineData("start start", AlignmentKind.Start, AlignmentKind.Start)]   // two explicit tracks
        [InlineData("none center", null, AlignmentKind.Center)]                 // one explicit null, one explicit track
        [InlineData("end", AlignmentKind.End, AlignmentKind.End)]               // two implicit tracks
        [InlineData("none", null, null)]                                        // two implicit nulls
        [InlineData("left", AlignmentKind.Start, null)]                         // one implicit column
        [InlineData("left center", AlignmentKind.Start, AlignmentKind.Center)]  // one explicit column, one explicit track
        [InlineData("bottom", null, AlignmentKind.End)]                         // one explicit row
        [InlineData("bottom right", AlignmentKind.End, AlignmentKind.End)]      // one explicit row, one explicit column
        [InlineData("start top", AlignmentKind.Start, AlignmentKind.Start)]     // one explicit track, one explicit row
        [InlineData("none top", null, AlignmentKind.Start)]                     // one explicit null, one explicit row
        public void AlignShorthand(string value, AlignmentKind? expectedHorizontal, AlignmentKind? expectedVertical)
        {
            var tokens = tokenizer.Tokenize($"align={value}");
            var result = AttributeParsers.NodeAlignAttribute(tokens);

            Assert.True(result.HasValue, result.ToString());
            Assert.Equal(expectedHorizontal, ((AST.NodeAlignAttribute)result.Value).Columns);
            Assert.Equal(expectedVertical, ((AST.NodeAlignAttribute)result.Value).Rows);
        }
    }
}
