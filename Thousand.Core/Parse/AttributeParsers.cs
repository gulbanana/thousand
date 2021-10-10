using Superpower;
using Superpower.Model;
using Superpower.Parsers;
using System.Linq;
using Thousand.Model;
using Thousand.Parse.Attributes;

namespace Thousand.Parse
{
    public static class AttributeParsers
    {
        #region utilities and combinators
        private static TokenListParser<TokenKind, Token<TokenKind>> Key<TK>(TK kind) where TK : struct, System.Enum =>
            Identifier.EnumValue(kind)
                 .IgnoreThen(Token.EqualTo(TokenKind.EqualsSign));

        private static TokenListParser<TokenKind, (T1?, T2?, T3?)> Shorthand<T1, T2, T3>(TokenListParser<TokenKind, T1> p1, TokenListParser<TokenKind, T2> p2, TokenListParser<TokenKind, T3> p3)
            where T1: class
            where T2 : class
            where T3: struct
        => (TokenList<TokenKind> input) =>
        {
            var x1 = default(T1?);
            var x2 = default(T2?);
            var x3 = default(T3?);
            
            var remainder = input;
            while (!remainder.IsAtEnd && (x1 == null || x2 == null || !x3.HasValue))
            {
                var next = remainder.ConsumeToken();

                var asT1 = p1.Try()(remainder);
                var asT2 = p2.Try()(remainder);
                var asT3 = p3.Try()(remainder);

                if (asT1.HasValue)
                {
                    x1 = asT1.Value;
                }
                else if (asT2.HasValue)
                {
                    x2 = asT2.Value;
                }
                else if (asT3.HasValue)
                {
                    x3 = asT3.Value;
                }
                else
                {
                    break;
                }

                remainder = next.Remainder;
            }

            if (x1 != null || x2 != null || x3.HasValue)
            {
                return TokenListParserResult.Value((x1, x2, x3), input, remainder);
            }
            else
            {
                var singleResult = p1.Value(Unit.Value).Or(p2.Value(Unit.Value)).Or(p3.Value(Unit.Value))(input);
                return TokenListParserResult.CastEmpty<TokenKind, Unit, (T1?, T2?, T3?)>(singleResult);
            }
        };
        #endregion

        #region arrow group, used only by edges
        public static TokenListParser<TokenKind, AST.ArrowAttribute> ArrowAnchorStartAttribute { get; } =
            from key in Key(ArrowAttributeKind.AnchorStart)
            from value in Value.Anchor
            select new AST.ArrowAnchorStartAttribute(value) as AST.ArrowAttribute;

        public static TokenListParser<TokenKind, AST.ArrowAttribute> ArrowAnchorEndAttribute { get; } =
            from key in Key(ArrowAttributeKind.AnchorEnd)
            from value in Value.Anchor
            select new AST.ArrowAnchorEndAttribute(value) as AST.ArrowAttribute;

        public static TokenListParser<TokenKind, AST.ArrowAttribute> ArrowAnchorAttribute { get; } =
            from key in Key(ArrowAttributeKind.Anchor)
            from startAndEnd in Value.Anchor.Twice()
            select new AST.ArrowAnchorAttribute(startAndEnd.first, startAndEnd.second) as AST.ArrowAttribute;

        public static TokenListParser<TokenKind, AST.ArrowAttribute> ArrowOffsetStartAttribute { get; } =
            from key in Key(ArrowAttributeKind.OffsetStart)
            from value in Value.Point
            select new AST.ArrowOffsetStartAttribute(value) as AST.ArrowAttribute;

        public static TokenListParser<TokenKind, AST.ArrowAttribute> ArrowOffsetEndAttribute { get; } =
            from key in Key(ArrowAttributeKind.OffsetEnd)
            from value in Value.Point
            select new AST.ArrowOffsetEndAttribute(value) as AST.ArrowAttribute;

        public static TokenListParser<TokenKind, AST.ArrowAttribute> ArrowOffsetAttribute { get; } =
            from key in Key(ArrowAttributeKind.Offset)
            from startAndEnd in Value.Point.Twice()
            select new AST.ArrowOffsetAttribute(startAndEnd.first, startAndEnd.second) as AST.ArrowAttribute;

        public static TokenListParser<TokenKind, AST.ArrowAttribute> ArrowAttribute { get; } =
            ArrowAnchorStartAttribute
                .Or(ArrowAnchorEndAttribute)
                .Or(ArrowAnchorAttribute)
                .Or(ArrowOffsetStartAttribute)
                .Or(ArrowOffsetEndAttribute)
                .Or(ArrowOffsetAttribute);
        #endregion

        #region doc group, used only by diagrams
        public static TokenListParser<TokenKind, AST.DocumentAttribute> DocumentScaleAttribute { get; } =
            from key in Key(DocumentAttributeKind.Scale)
            from value in Value.Decimal
            select new AST.DocumentScaleAttribute(value) as AST.DocumentAttribute;

        public static TokenListParser<TokenKind, AST.DocumentAttribute> DocumentAttribute { get; } =
            DocumentScaleAttribute;
        #endregion

        #region line (drawing) group, used by objects and lines (edges)
        public static TokenListParser<TokenKind, AST.LineAttribute> LineStrokeColourAttribute { get; } =
            from key in Key(LineAttributeKind.StrokeColour)
            from value in Value.Colour
            select new AST.LineStrokeColourAttribute(value) as AST.LineAttribute;

        public static TokenListParser<TokenKind, AST.LineAttribute> LineStrokeWidthAttribute { get; } =
            from key in Key(LineAttributeKind.StrokeWidth)
            from value in Value.Width
            select new AST.LineStrokeWidthAttribute(value) as AST.LineAttribute;

        public static TokenListParser<TokenKind, AST.LineAttribute> LineStrokeStyleAttribute { get; } =
            from key in Key(LineAttributeKind.StrokeStyle)
            from value in Identifier.Enum<StrokeKind>()
            select new AST.LineStrokeStyleAttribute(value) as AST.LineAttribute;

        public static TokenListParser<TokenKind, AST.LineAttribute> LineStrokeAttribute { get; } =
            from key in Key(LineAttributeKind.Stroke)
            from values in Shorthand(Value.Colour, Value.Width, Identifier.Enum<StrokeKind>())
            select values switch { (var c, var w, var s) => new AST.LineStrokeAttribute(c, s, w) as AST.LineAttribute };

        public static TokenListParser<TokenKind, AST.LineAttribute> StrokeAttribute { get; } =
            LineStrokeAttribute
                .Or(LineStrokeColourAttribute)
                .Or(LineStrokeWidthAttribute)
                .Or(LineStrokeStyleAttribute);
        #endregion

        #region text group, used only by objects (so far)
        public static TokenListParser<TokenKind, AST.TextAttribute> TextFontSizeAttribute { get; } =
            from key in Key(TextAttributeKind.FontSize)
            from value in Value.CountingNumber
            select new AST.TextFontSizeAttribute(value) as AST.TextAttribute;

        public static TokenListParser<TokenKind, AST.TextAttribute> TextFontFamilyAttribute { get; } =
            from key in Key(TextAttributeKind.FontFamily)
            from value in Value.String
            select new AST.TextFontFamilyAttribute(value) as AST.TextAttribute;

        public static TokenListParser<TokenKind, AST.TextAttribute> TextFontColourAttribute { get; } =
            from key in Key(TextAttributeKind.FontColour)
            from value in Value.Colour
            select new AST.TextFontColourAttribute(value) as AST.TextAttribute;

        public static TokenListParser<TokenKind, AST.TextAttribute> TextFontAttribute { get; } =
            from key in Key(TextAttributeKind.Font)
            from values in Shorthand(Value.Colour, Value.String, Value.CountingNumber)
            select values switch { (var c, var f, var s) => new AST.TextFontAttribute(f, s, c) as AST.TextAttribute };

        public static TokenListParser<TokenKind, AST.TextAttribute> TextAttribute { get; } =
            TextFontAttribute
                .Or(TextFontFamilyAttribute)
                .Or(TextFontSizeAttribute)
                .Or(TextFontColourAttribute);
        #endregion

        #region node group, used only by objects
        public static TokenListParser<TokenKind, AST.NodeAttribute> NodeLabelAttribute { get; } =
            from key in Key(NodeAttributeKind.Label)
            from value in Value.NullableString
            select new AST.NodeLabelAttribute(value) as AST.NodeAttribute;

        public static TokenListParser<TokenKind, AST.NodeAttribute> NodeRowAttribute { get; } =
            from key in Key(NodeAttributeKind.Row)
            from value in Value.CountingNumber
            select new AST.NodeRowAttribute(value) as AST.NodeAttribute;

        public static TokenListParser<TokenKind, AST.NodeAttribute> NodeColumnAttribute { get; } =
            from key in Key(NodeAttributeKind.Col)
            from value in Value.CountingNumber
            select new AST.NodeColumnAttribute(value) as AST.NodeAttribute;

        public static TokenListParser<TokenKind, AST.NodeAttribute> NodeMinWidthAttribute { get; } =
            from key in Key(NodeAttributeKind.MinWidth)
            from value in Value.CountingNumber
            select new AST.NodeMinWidthAttribute(value) as AST.NodeAttribute;

        public static TokenListParser<TokenKind, AST.NodeAttribute> NodeMinHeightAttribute { get; } =
            from key in Key(NodeAttributeKind.MinHeight)
            from value in Value.CountingNumber
            select new AST.NodeMinHeightAttribute(value) as AST.NodeAttribute;

        public static TokenListParser<TokenKind, AST.NodeAttribute> NodeShapeAttribute { get; } =
            from key in Key(NodeAttributeKind.Shape)
            from value in Identifier.Enum<ShapeKind>().OrNone()
            select new AST.NodeShapeAttribute(value) as AST.NodeAttribute;

        public static TokenListParser<TokenKind, AST.NodeAttribute> NodeAlignRowAttribute { get; } =
            from key in Key(NodeAttributeKind.AlignVertical)
            from value in Identifier.Enum<AlignmentKind>().OrNone()
            select new AST.NodeAlignRowAttribute(value) as AST.NodeAttribute;

        public static TokenListParser<TokenKind, AST.NodeAttribute> NodeAlignColumnAttribute { get; } =
            from key in Key(NodeAttributeKind.AlignHorizontal)
            from value in Identifier.Enum<AlignmentKind>().OrNone()
            select new AST.NodeAlignColumnAttribute(value) as AST.NodeAttribute;

        public static TokenListParser<TokenKind, AST.NodeAttribute> NodeAlignAttribute { get; } =
            from key in Key(NodeAttributeKind.Align)
            from value in Identifier.Enum<AlignmentKind>().OrNone().Twice()
            select new AST.NodeAlignAttribute(value.first, value.second) as AST.NodeAttribute;

        public static TokenListParser<TokenKind, AST.NodeAttribute> NodeMarginAttribute { get; } =
            from key in Key(NodeAttributeKind.Margin)
            from value in Value.Border
            select new AST.NodeMarginAttribute(value) as AST.NodeAttribute;

        public static TokenListParser<TokenKind, AST.NodeAttribute> NodeCornerRadiusAttribute { get; } =
            from key in Key(NodeAttributeKind.Corner)
            from value in Value.WholeNumber
            select new AST.NodeCornerRadiusAttribute(value) as AST.NodeAttribute;

        public static TokenListParser<TokenKind, AST.NodeAttribute> NodeAttribute { get; } =
            NodeLabelAttribute
                .Or(NodeShapeAttribute)
                .Or(NodeAlignColumnAttribute)
                .Or(NodeAlignRowAttribute)
                .Or(NodeAlignAttribute)
                .Or(NodeMarginAttribute)
                .Or(NodeCornerRadiusAttribute)
                .Or(NodeColumnAttribute)
                .Or(NodeRowAttribute)                
                .Or(NodeMinWidthAttribute)
                .Or(NodeMinHeightAttribute);
        #endregion

        #region region group, used by objects and diagrams
        public static TokenListParser<TokenKind, AST.RegionAttribute> RegionFillAttribute { get; } =
            from key in Key(RegionAttributeKind.Fill)
            from value in Value.Colour.OrNull()
            select new AST.RegionFillAttribute(value) as AST.RegionAttribute;

        public static TokenListParser<TokenKind, AST.RegionAttribute> RegionPaddingAttribute { get; } =
            from key in Key(RegionAttributeKind.Padding)
            from value in Value.Border
            select new AST.RegionPaddingAttribute(value) as AST.RegionAttribute;

        public static TokenListParser<TokenKind, AST.RegionAttribute> RegionLayoutAttribute { get; } =
            from key in Key(RegionAttributeKind.Layout)
            from value in Identifier.Enum<LayoutKind>()
            select new AST.RegionLayoutAttribute(value) as AST.RegionAttribute;

        public static TokenListParser<TokenKind, AST.RegionAttribute> RegionGridFlowAttribute { get; } =
            from key in Key(RegionAttributeKind.GridFlow)
            from value in Identifier.Enum<FlowKind>()
            select new AST.RegionGridFlowAttribute(value) as AST.RegionAttribute;

        public static TokenListParser<TokenKind, AST.RegionAttribute> RegionSpaceColumnsAttribute { get; } =
            from key in Key(RegionAttributeKind.SpaceColumns)
            from value in Value.WholeNumber
            select new AST.RegionSpaceColumnsAttribute(value) as AST.RegionAttribute;

        public static TokenListParser<TokenKind, AST.RegionAttribute> RegionSpaceRowsAttribute { get; } =
            from key in Key(RegionAttributeKind.SpaceRows)
            from value in Value.WholeNumber
            select new AST.RegionSpaceRowsAttribute(value) as AST.RegionAttribute;

        public static TokenListParser<TokenKind, AST.RegionAttribute> RegionSpaceAttribute { get; } =
            from key in Key(RegionAttributeKind.Space)
            from columnsAndRows in Value.WholeNumber.Twice()
            select new AST.RegionSpaceAttribute(columnsAndRows.first, columnsAndRows.second) as AST.RegionAttribute;

        public static TokenListParser<TokenKind, AST.RegionAttribute> RegionPackColumnsAttribute { get; } =
            from key in Key(RegionAttributeKind.PackColumns)
            from value in Value.TrackSize
            select new AST.RegionPackColumnsAttribute(value) as AST.RegionAttribute;

        public static TokenListParser<TokenKind, AST.RegionAttribute> RegionPackRowsAttribute { get; } =
            from key in Key(RegionAttributeKind.PackRows)
            from value in Value.TrackSize
            select new AST.RegionPackRowsAttribute(value) as AST.RegionAttribute;

        public static TokenListParser<TokenKind, AST.RegionAttribute> RegionPackAttribute { get; } =
            from key in Key(RegionAttributeKind.Pack)
            from columnsAndRows in Value.TrackSize.Twice()
            select new AST.RegionPackAttribute(columnsAndRows.first, columnsAndRows.second) as AST.RegionAttribute;

        public static TokenListParser<TokenKind, AST.RegionAttribute> RegionJustifyColumnsAttribute { get; } =
            from key in Key(RegionAttributeKind.JustifyColumns)
            from value in Identifier.Enum<AlignmentKind>()
            select new AST.RegionJustifyColumnsAttribute(value) as AST.RegionAttribute;

        public static TokenListParser<TokenKind, AST.RegionAttribute> RegionJustifyRowsAttribute { get; } =
            from key in Key(RegionAttributeKind.JustifyRows)
            from value in Identifier.Enum<AlignmentKind>()
            select new AST.RegionJustifyRowsAttribute(value) as AST.RegionAttribute;

        public static TokenListParser<TokenKind, AST.RegionAttribute> RegionJustifyAttribute { get; } =
            from key in Key(RegionAttributeKind.Justify)
            from columnsAndRows in Identifier.Enum<AlignmentKind>().Twice()
            select new AST.RegionJustifyAttribute(columnsAndRows.first, columnsAndRows.second) as AST.RegionAttribute;

        public static TokenListParser<TokenKind, AST.RegionAttribute> RegionAttribute { get; } =
            RegionFillAttribute
                .Or(RegionPaddingAttribute)
                .Or(RegionLayoutAttribute)
                .Or(RegionGridFlowAttribute)
                .Or(RegionSpaceColumnsAttribute)
                .Or(RegionSpaceRowsAttribute)
                .Or(RegionSpaceAttribute)
                .Or(RegionPackColumnsAttribute)
                .Or(RegionPackRowsAttribute)
                .Or(RegionPackAttribute)
                .Or(RegionJustifyColumnsAttribute)
                .Or(RegionJustifyRowsAttribute)
                .Or(RegionJustifyAttribute);
        #endregion
    }
}
