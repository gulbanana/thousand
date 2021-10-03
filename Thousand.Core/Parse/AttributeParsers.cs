using Superpower;
using Superpower.Model;
using Superpower.Parsers;
using System.Linq;
using Thousand.Model;

namespace Thousand.Parse
{
    public static class AttributeParsers
    {
        #region utilities and types
        private static TokenListParser<TokenKind, Token<TokenKind>> Key<TK>(TK kind) where TK : struct =>
            Token.EqualToValueIgnoreCase(TokenKind.Identifier, kind.ToString()!)
                 .IgnoreThen(Token.EqualTo(TokenKind.EqualsSign));

        private static TokenListParser<TokenKind, Token<TokenKind>> Keys<TK>(params TK[] kinds) where TK : struct =>
            kinds.Select(k => Token.EqualToValueIgnoreCase(TokenKind.Identifier, k.ToString()!))
                 .Aggregate((p1, p2) => p1.Or(p2))
                 .IgnoreThen(Token.EqualTo(TokenKind.EqualsSign));

        public static TokenListParser<TokenKind, int> CountingNumberValue { get; } =
            Token.EqualTo(TokenKind.Number).Apply(TextParsers.CountingNumber);

        public static TokenListParser<TokenKind, int> WholeNumberValue { get; } =
            Token.EqualTo(TokenKind.Number).Apply(TextParsers.WholeNumber);

        public static TokenListParser<TokenKind, int> IntegerValue { get; } =
            Token.EqualTo(TokenKind.Number).Apply(Numerics.IntegerInt32);

        public static TokenListParser<TokenKind, decimal> DecimalValue { get; } =
            Token.EqualTo(TokenKind.Number).Apply(Numerics.DecimalDecimal);

        public static TokenListParser<TokenKind, string?> NullableStringValue { get; } =
            Token.EqualTo(TokenKind.String).Apply(TextParsers.String).AsNullable()
                .Or(Token.EqualTo(TokenKind.NoneKeyword).Value(default(string?)));

        public static TokenListParser<TokenKind, Colour> ColourValue { get; } =
            Token.EqualTo(TokenKind.Colour).Apply(TextParsers.Colour)
                .Or(Identifier.Statics<Colour>());

        public static TokenListParser<TokenKind, Width> WidthValue { get; } =
            Token.EqualToValueIgnoreCase(TokenKind.Identifier, "hairline").Value(new HairlineWidth() as Width)
                .Or(WholeNumberValue.OrNone().Select(x => x.HasValue ? new PositiveWidth(x.Value) : new ZeroWidth() as Width));
        #endregion

        #region arrow group, used only by edges
        public static TokenListParser<TokenKind, AST.ArrowAttribute> ArrowAnchorStartAttribute { get; } =
            from key in Key(ArrowAttributeKind.AnchorStart)
            from value in Identifier.Enum<AnchorKind>().OrNone()
            select new AST.ArrowAnchorStartAttribute(value) as AST.ArrowAttribute;

        public static TokenListParser<TokenKind, AST.ArrowAttribute> ArrowAnchorEndAttribute { get; } =
            from key in Key(ArrowAttributeKind.AnchorEnd)
            from value in Identifier.Enum<AnchorKind>().OrNone()
            select new AST.ArrowAnchorEndAttribute(value) as AST.ArrowAttribute;

        public static TokenListParser<TokenKind, AST.ArrowAttribute> ArrowAnchorAttribute { get; } =
            from key in Key(ArrowAttributeKind.Anchor)
            from value in Identifier.Enum<AnchorKind>().OrNone()
            select new AST.ArrowAnchorAttribute(value) as AST.ArrowAttribute;

        public static TokenListParser<TokenKind, AST.ArrowAttribute> ArrowOffsetStartXAttribute { get; } =
            from key in Key(ArrowAttributeKind.OffsetStartX)
            from value in IntegerValue
            select new AST.ArrowOffsetStartXAttribute(value) as AST.ArrowAttribute;

        public static TokenListParser<TokenKind, AST.ArrowAttribute> ArrowOffsetStartYAttribute { get; } =
            from key in Key(ArrowAttributeKind.OffsetStartY)
            from value in IntegerValue
            select new AST.ArrowOffsetStartYAttribute(value) as AST.ArrowAttribute;

        public static TokenListParser<TokenKind, AST.ArrowAttribute> ArrowOffsetEndXAttribute { get; } =
            from key in Key(ArrowAttributeKind.OffsetEndX)
            from value in IntegerValue
            select new AST.ArrowOffsetEndXAttribute(value) as AST.ArrowAttribute;

        public static TokenListParser<TokenKind, AST.ArrowAttribute> ArrowOffsetEndYAttribute { get; } =
            from key in Key(ArrowAttributeKind.OffsetEndY)
            from value in IntegerValue
            select new AST.ArrowOffsetEndYAttribute(value) as AST.ArrowAttribute;

        public static TokenListParser<TokenKind, AST.ArrowAttribute> ArrowOffsetXAttribute { get; } =
            from key in Key(ArrowAttributeKind.OffsetX)
            from value in IntegerValue
            select new AST.ArrowOffsetXAttribute(value) as AST.ArrowAttribute;

        public static TokenListParser<TokenKind, AST.ArrowAttribute> ArrowOffsetYAttribute { get; } =
            from key in Key(ArrowAttributeKind.OffsetY)
            from value in IntegerValue
            select new AST.ArrowOffsetYAttribute(value) as AST.ArrowAttribute;

        public static TokenListParser<TokenKind, AST.ArrowAttribute> ArrowAttribute { get; } =
            ArrowAnchorStartAttribute
                .Or(ArrowAnchorEndAttribute)
                .Or(ArrowAnchorAttribute)
                .Or(ArrowOffsetStartXAttribute)
                .Or(ArrowOffsetStartYAttribute)
                .Or(ArrowOffsetEndXAttribute)
                .Or(ArrowOffsetEndYAttribute)
                .Or(ArrowOffsetXAttribute)
                .Or(ArrowOffsetYAttribute);
        #endregion

        #region doc group, used only by diagrams
        public static TokenListParser<TokenKind, AST.DocumentAttribute> DocumentScaleAttribute { get; } =
            from key in Key(DocumentAttributeKind.Scale)
            from value in DecimalValue
            select new AST.DocumentScaleAttribute(value) as AST.DocumentAttribute;

        public static TokenListParser<TokenKind, AST.DocumentAttribute> DocumentAttribute { get; } =
            DocumentScaleAttribute;
        #endregion

        #region stroke group, used by objects and lines
        public static TokenListParser<TokenKind, AST.StrokeAttribute> StrokeColourAttribute { get; } =
            from key in Key(LineAttributeKind.StrokeColour)
            from value in ColourValue
            select new AST.StrokeColourAttribute(value) as AST.StrokeAttribute;

        public static TokenListParser<TokenKind, AST.StrokeAttribute> StrokeWidthAttribute { get; } =
            from key in Key(LineAttributeKind.StrokeWidth)
            from value in WidthValue
            select new AST.StrokeWidthAttribute(value) as AST.StrokeAttribute;

        public static TokenListParser<TokenKind, AST.StrokeAttribute> StrokeStyleAttribute { get; } =
            from key in Key(LineAttributeKind.StrokeStyle)
            from value in Identifier.Enum<StrokeKind>()
            select new AST.StrokeStyleAttribute(value) as AST.StrokeAttribute;

        private static TokenListParserResult<TokenKind, AST.StrokeShorthandAttribute> StrokeValues(TokenList<TokenKind> input)
        {
            Colour? s1 = null;
            StrokeKind? s2 = null;
            Width? s3 = null;

            var remainder = input;
            while (!remainder.IsAtEnd && (s1 == null || s2 == null || s3 == null))
            {
                var next = remainder.ConsumeToken();

                var asColour = ColourValue.Try()(remainder);
                var asStyle = Identifier.Enum<StrokeKind>().Try()(remainder);
                var asWidth = WidthValue.Try()(remainder);

                if (asColour.HasValue)
                {
                    s1 = asColour.Value;
                }                               
                else if (asStyle.HasValue)
                {
                    s2 = asStyle.Value;
                }
                else if (asWidth.HasValue)
                {
                    s3 = asWidth.Value;
                }
                else
                {
                    break;
                }

                remainder = next.Remainder;
            }

            if (s1 != null || s2 != null || s3 != null)
            {
                return TokenListParserResult.Value<TokenKind, AST.StrokeShorthandAttribute>(new AST.StrokeShorthandAttribute(s1, s2, s3), input, remainder);
            }
            else
            {
                return TokenListParserResult.Empty<TokenKind, AST.StrokeShorthandAttribute>(input);
            }
        }

        public static TokenListParser<TokenKind, AST.StrokeAttribute> StrokeShorthandAttribute { get; } =
            from key in Key(LineAttributeKind.Stroke)
            from value in new TokenListParser<TokenKind, AST.StrokeShorthandAttribute>(StrokeValues)
            select value as AST.StrokeAttribute;

        public static TokenListParser<TokenKind, AST.StrokeAttribute> StrokeAttribute { get; } =
            StrokeShorthandAttribute
                .Or(StrokeColourAttribute)
                .Or(StrokeWidthAttribute)
                .Or(StrokeStyleAttribute);
        #endregion

        #region text group, used only by objects (so far)
        public static TokenListParser<TokenKind, AST.TextAttribute> TextLabelAttribute { get; } =
            from key in Key(TextAttributeKind.Label)
            from value in NullableStringValue
            select new AST.TextLabelAttribute(value) as AST.TextAttribute;

        public static TokenListParser<TokenKind, AST.TextAttribute> TextFontSizeAttribute { get; } =
            from key in Key(TextAttributeKind.FontSize)
            from value in CountingNumberValue
            select new AST.TextFontSizeAttribute(value) as AST.TextAttribute;

        public static TokenListParser<TokenKind, AST.TextAttribute> TextFontFamilyAttribute { get; } =
            from key in Key(TextAttributeKind.FontFamily)
            from value in Parser.String
            select new AST.TextFontFamilyAttribute(value) as AST.TextAttribute;

        public static TokenListParser<TokenKind, AST.TextAttribute> TextAttribute { get; } =
            TextLabelAttribute
                .Or(TextFontFamilyAttribute)
                .Or(TextFontSizeAttribute);
        #endregion

        #region node group, used only by objects
        public static TokenListParser<TokenKind, AST.NodeAttribute> NodeRowAttribute { get; } =
            from key in Key(NodeAttributeKind.Row)
            from value in CountingNumberValue
            select new AST.NodeRowAttribute(value) as AST.NodeAttribute;

        public static TokenListParser<TokenKind, AST.NodeAttribute> NodeColumnAttribute { get; } =
            from key in Keys(NodeAttributeKind.Col, NodeAttributeKind.Column)
            from value in CountingNumberValue
            select new AST.NodeColumnAttribute(value) as AST.NodeAttribute;

        public static TokenListParser<TokenKind, AST.NodeAttribute> NodeWidthAttribute { get; } =
            from key in Key(NodeAttributeKind.Width)
            from value in CountingNumberValue
            select new AST.NodeWidthAttribute(value) as AST.NodeAttribute;

        public static TokenListParser<TokenKind, AST.NodeAttribute> NodeHeightAttribute { get; } =
            from key in Key(NodeAttributeKind.Height)
            from value in CountingNumberValue
            select new AST.NodeHeightAttribute(value) as AST.NodeAttribute;

        public static TokenListParser<TokenKind, AST.NodeAttribute> NodeShapeAttribute { get; } =
            from key in Key(NodeAttributeKind.Shape)
            from value in Identifier.Enum<ShapeKind>().OrNone()
            select new AST.NodeShapeAttribute(value) as AST.NodeAttribute;

        public static TokenListParser<TokenKind, AST.NodeAttribute> NodePaddingAttribute { get; } =
            from key in Key(NodeAttributeKind.Padding)
            from value in WholeNumberValue
            select new AST.NodePaddingAttribute(value) as AST.NodeAttribute;

        public static TokenListParser<TokenKind, AST.NodeAttribute> NodeCornerRadiusAttribute { get; } =
            from key in Keys(NodeAttributeKind.Corner, NodeAttributeKind.CornerRadius)
            from value in WholeNumberValue
            select new AST.NodeCornerRadiusAttribute(value) as AST.NodeAttribute;

        public static TokenListParser<TokenKind, AST.NodeAttribute> NodeAttribute { get; } =
            NodeShapeAttribute
                .Or(NodePaddingAttribute)
                .Or(NodeCornerRadiusAttribute)
                .Or(NodeRowAttribute)
                .Or(NodeColumnAttribute)
                .Or(NodeWidthAttribute)
                .Or(NodeHeightAttribute);
        #endregion

        #region region group, used by objects and diagrams
        public static TokenListParser<TokenKind, AST.RegionAttribute> RegionFillAttribute { get; } =
            from key in Key(RegionAttributeKind.Fill)
            from value in ColourValue.OrNull()
            select new AST.RegionFillAttribute(value) as AST.RegionAttribute;

        public static TokenListParser<TokenKind, AST.RegionAttribute> RegionLayoutAttribute { get; } =
            from key in Key(RegionAttributeKind.Layout)
            from value in Identifier.Enum<LayoutKind>()
            select new AST.RegionLayoutAttribute(value) as AST.RegionAttribute;

        public static TokenListParser<TokenKind, AST.RegionAttribute> RegionMarginAttribute { get; } =
            from key in Key(RegionAttributeKind.Margin)
            from value in WholeNumberValue
            select new AST.RegionMarginAttribute(value) as AST.RegionAttribute;

        public static TokenListParser<TokenKind, AST.RegionAttribute> RegionGutterAttribute { get; } =
            from key in Key(RegionAttributeKind.Gutter)
            from value in WholeNumberValue
            select new AST.RegionGutterAttribute(value) as AST.RegionAttribute;

        public static TokenListParser<TokenKind, AST.RegionAttribute> RegionAttribute { get; } =
            RegionFillAttribute
                .Or(RegionLayoutAttribute)
                .Or(RegionMarginAttribute)
                .Or(RegionGutterAttribute);
        #endregion
    }
}
