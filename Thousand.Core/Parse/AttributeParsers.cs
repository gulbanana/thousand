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
            Token.EqualToValueIgnoreCase(TokenKind.Keyword, kind.ToString()!)
                 .IgnoreThen(Token.EqualTo(TokenKind.EqualsSign));

        private static TokenListParser<TokenKind, Token<TokenKind>> Keys<TK>(params TK[] kinds) where TK : struct =>
            kinds.Select(k => Token.EqualToValueIgnoreCase(TokenKind.Keyword, k.ToString()!))
                 .Aggregate((p1, p2) => p1.Or(p2))
                 .IgnoreThen(Token.EqualTo(TokenKind.EqualsSign));

        public static TokenListParser<TokenKind, int> CountingNumberValue { get; } =
            Token.EqualTo(TokenKind.Number).Apply(TextParsers.CountingNumber);

        public static TokenListParser<TokenKind, int> WholeNumberValue { get; } =
            Token.EqualTo(TokenKind.Number).Apply(TextParsers.WholeNumber);

        public static TokenListParser<TokenKind, int> IntegerValue { get; } =
            Token.EqualTo(TokenKind.Number).Apply(Numerics.IntegerInt32);

        public static TokenListParser<TokenKind, float> DecimalValue { get; } =
            Token.EqualTo(TokenKind.Number).Apply(TextParsers.DecimalFloat);

        public static TokenListParser<TokenKind, Point> PointValue { get; } =
            from begin in Token.EqualTo(TokenKind.LeftParenthesis)
            from x in IntegerValue
            from comma in Token.EqualTo(TokenKind.Comma)
            from y in IntegerValue
            from end in Token.EqualTo(TokenKind.RightParenthesis)
            select new Point(x, y);

        public static TokenListParser<TokenKind, Colour> ColourValue { get; } =
            Token.EqualTo(TokenKind.Colour).Apply(TextParsers.Colour)
                .Or(Keyword.Statics<Colour>());

        public static TokenListParser<TokenKind, string?> NullableStringValue { get; } =
            Token.EqualTo(TokenKind.String).Apply(TextParsers.String).AsNullable()
                .Or(Token.EqualTo(TokenKind.None).Value(default(string?)));
        #endregion

        #region arrow group, used only by edges
        public static TokenListParser<TokenKind, AST.ArrowAttribute> ArrowOffsetStartAttribute { get; } =
            from key in Key(ArrowAttributeKind.OffsetStart)
            from value in PointValue
            select new AST.ArrowOffsetStartAttribute(value) as AST.ArrowAttribute;

        public static TokenListParser<TokenKind, AST.ArrowAttribute> ArrowOffsetEndAttribute { get; } =
            from key in Key(ArrowAttributeKind.OffsetEnd)
            from value in PointValue
            select new AST.ArrowOffsetEndAttribute(value) as AST.ArrowAttribute;

        public static TokenListParser<TokenKind, AST.ArrowAttribute> ArrowOffsetBothAttribute { get; } =
            from key in Key(ArrowAttributeKind.Offset)
            from value in PointValue
            select new AST.ArrowOffsetBothAttribute(value) as AST.ArrowAttribute;

        public static TokenListParser<TokenKind, AST.ArrowAttribute> ArrowAttribute { get; } =
            ArrowOffsetStartAttribute
                .Or(ArrowOffsetEndAttribute)
                .Or(ArrowOffsetBothAttribute);
        #endregion

        #region doc group, used only by diagrams
        public static TokenListParser<TokenKind, AST.DocumentAttribute> DocumentScaleAttribute { get; } =
            from key in Key(DocumentAttributeKind.Scale)
            from value in DecimalValue
            select new AST.DocumentScaleAttribute(value) as AST.DocumentAttribute;

        public static TokenListParser<TokenKind, AST.DocumentAttribute> DocumentAttribute { get; } =
            DocumentScaleAttribute;
        #endregion

        #region line group, used by objects and edges
        public static TokenListParser<TokenKind, AST.LineAttribute> LineStrokeAttribute { get; } =
            from key in Key(LineAttributeKind.Stroke)
            from value in ColourValue
            select new AST.LineStrokeAttribute(value) as AST.LineAttribute;

        public static TokenListParser<TokenKind, AST.LineAttribute> LineWidthAttribute { get; } =
            from key in Key(LineAttributeKind.StrokeWidth)
            from value in WholeNumberValue.OrNone()
            select new AST.LineWidthAttribute(value) as AST.LineAttribute;

        public static TokenListParser<TokenKind, AST.LineAttribute> LineAttribute { get; } =
            LineStrokeAttribute
                .Or(LineWidthAttribute);
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

        public static TokenListParser<TokenKind, AST.TextAttribute> TextAttribute { get; } =
            TextLabelAttribute
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
            from value in Keyword.Enum<ShapeKind>()
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
            from value in ColourValue
            select new AST.RegionFillAttribute(value) as AST.RegionAttribute;

        public static TokenListParser<TokenKind, AST.RegionAttribute> RegionLayoutAttribute { get; } =
            from key in Key(RegionAttributeKind.Layout)
            from value in Keyword.Enum<LayoutKind>()
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
