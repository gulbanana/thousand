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

        private static TokenListParser<TokenKind, (T1?, T2?)> Shorthand<T1, T2>(TokenListParser<TokenKind, T1> p1, TokenListParser<TokenKind, T2> p2)
            where T1 : struct
            where T2 : struct
        => (TokenList<TokenKind> input) =>
        {
            var x1 = default(T1?);
            var x2 = default(T2?);

            var remainder = input;
            while (!remainder.IsAtEnd && (!x1.HasValue || !x2.HasValue))
            {
                var next = remainder.ConsumeToken();

                var asT1 = p1.Try()(remainder);
                var asT2 = p2.Try()(remainder);

                if (asT1.HasValue)
                {
                    x1 = asT1.Value;
                }
                else if (asT2.HasValue)
                {
                    x2 = asT2.Value;
                }
                else
                {
                    break;
                }

                remainder = next.Remainder;
            }

            if (x1.HasValue || x2.HasValue)
            {
                return TokenListParserResult.Value((x1, x2), input, remainder);
            }
            else
            {
                var singleResult = p1.Value(Unit.Value).Or(p2.Value(Unit.Value)).Or(p2.Value(Unit.Value))(input);
                return TokenListParserResult.CastEmpty<TokenKind, Unit, (T1?, T2?)>(singleResult);
            }
        };

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

        #region special alignment shorthand helpers
        private static TokenListParser<TokenKind, AlignmentKind> AlignColumnOnly { get; } =
            Token.EqualToValueIgnoreCase(TokenKind.Identifier, "left").Value(AlignmentKind.Start)
                .Or(Token.EqualToValueIgnoreCase(TokenKind.Identifier, "right").Value(AlignmentKind.End));

        private static TokenListParser<TokenKind, AlignmentKind> AlignColumn { get; } =
            AlignColumnOnly.Or(Identifier.Enum<AlignmentKind>());

        private static TokenListParser<TokenKind, AlignmentKind> AlignRowOnly { get; } =
            Token.EqualToValueIgnoreCase(TokenKind.Identifier, "top").Value(AlignmentKind.Start)
                .Or(Token.EqualToValueIgnoreCase(TokenKind.Identifier, "bottom").Value(AlignmentKind.End));

        private static TokenListParser<TokenKind, AlignmentKind> AlignRow { get; } =
            AlignRowOnly.Or(Identifier.Enum<AlignmentKind>());
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
            from key in Key(EntityAttributeKind.Anchor)
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
            from key in Key(EntityAttributeKind.Offset)
            from startAndEnd in Value.Point.Twice()
            select new AST.ArrowOffsetAttribute(startAndEnd.first, startAndEnd.second) as AST.ArrowAttribute;

        public static TokenListParser<TokenKind, AST.ArrowAttribute> ArrowAttribute { get; } =
            ArrowAnchorStartAttribute
                .Or(ArrowAnchorEndAttribute)
                .Or(ArrowAnchorAttribute)
                .Or(ArrowOffsetStartAttribute)
                .Or(ArrowOffsetEndAttribute)
                .Or(ArrowOffsetAttribute);

        public static TokenListParser<TokenKind, AST.ArrowAttribute> ArrowOnlyAnchorAttribute { get; } =
            from key in Key(EntityAttributeKind.Anchor)
            from start in Value.Anchor
            from end in Value.Anchor
            select new AST.ArrowAnchorAttribute(start, end) as AST.ArrowAttribute;

        public static TokenListParser<TokenKind, AST.ArrowAttribute> ArrowOnlyOffsetAttribute { get; } =
            from key in Key(EntityAttributeKind.Offset)
            from start in Value.Point
            from end in Value.Point
            select new AST.ArrowOffsetAttribute(start, end) as AST.ArrowAttribute;

        public static TokenListParser<TokenKind, AST.ArrowAttribute> ArrowOnlyAttribute { get; } =
            ArrowAnchorStartAttribute
                .Or(ArrowAnchorEndAttribute)
                .Or(ArrowOnlyAnchorAttribute)
                .Or(ArrowOffsetStartAttribute)
                .Or(ArrowOffsetEndAttribute)
                .Or(ArrowOnlyOffsetAttribute);
        #endregion

        #region doc group, used only by diagrams
        public static TokenListParser<TokenKind, AST.DocumentAttribute> DocumentScaleAttribute { get; } =
            from key in Key(DocumentAttributeKind.Scale)
            from value in Value.Decimal
            select new AST.DocumentScaleAttribute(value) as AST.DocumentAttribute;

        public static TokenListParser<TokenKind, AST.DocumentAttribute> DocumentAttribute { get; } =
            DocumentScaleAttribute;
        #endregion

        // XXX these could be merged
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

        public static TokenListParser<TokenKind, AST.LineAttribute> LineAttribute { get; } =
            LineStrokeAttribute
                .Or(LineStrokeColourAttribute)
                .Or(LineStrokeWidthAttribute)
                .Or(LineStrokeStyleAttribute);
        #endregion

        #region entity position group, used by objects and lines (edges)
        public static TokenListParser<TokenKind, AST.PositionAttribute> EntityAnchorAttribute { get; } =
            from key in Key(EntityAttributeKind.Anchor)
            from value in Identifier.Enum<CompassKind>()
            select new AST.PositionAnchorAttribute(value) as AST.PositionAttribute;

        public static TokenListParser<TokenKind, AST.PositionAttribute> EntityOffsetAttribute { get; } =
            from key in Key(EntityAttributeKind.Offset)
            from value in Value.Point
            select new AST.PositionOffsetAttribute(value) as AST.PositionAttribute;

        public static TokenListParser<TokenKind, AST.PositionAttribute> PositionAttribute { get; } =
            EntityAnchorAttribute
                .Or(EntityOffsetAttribute);
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
        public static TokenListParser<TokenKind, AST.NodeAttribute> NodeLabelContentAttribute { get; } =
            from key in Key(NodeAttributeKind.LabelContent)
            from value in Value.Text
            select new AST.NodeLabelContentAttribute(value) as AST.NodeAttribute;

        public static TokenListParser<TokenKind, AST.NodeAttribute> NodeLabelJustifyAttribute { get; } =
            from key in Key(NodeAttributeKind.LabelJustify)
            from value in AlignColumn
            select new AST.NodeLabelJustifyAttribute(value) as AST.NodeAttribute;

        public static TokenListParser<TokenKind, AST.NodeAttribute> NodeLabelAttribute { get; } =
            from key in Key(NodeAttributeKind.Label)
            from values in Shorthand(Identifier.Enum<AlignmentKind>(), Value.Text)
            select values switch { (var a, var t) => new AST.NodeLabelAttribute(t, a) as AST.NodeAttribute };

        public static TokenListParser<TokenKind, AST.NodeAttribute> NodeColumnAttribute { get; } =
            from key in Key(NodeAttributeKind.Col)
            from value in Value.CountingNumber
            select new AST.NodeColumnAttribute(value) as AST.NodeAttribute;

        public static TokenListParser<TokenKind, AST.NodeAttribute> NodeRowAttribute { get; } =
            from key in Key(NodeAttributeKind.Row)
            from value in Value.CountingNumber
            select new AST.NodeRowAttribute(value) as AST.NodeAttribute;

        public static TokenListParser<TokenKind, AST.NodeAttribute> NodeXAttribute { get; } =
            from key in Key(NodeAttributeKind.X)
            from value in Value.WholeNumber
            select new AST.NodeXAttribute(value) as AST.NodeAttribute;

        public static TokenListParser<TokenKind, AST.NodeAttribute> NodeYAttribute { get; } =
            from key in Key(NodeAttributeKind.X)
            from value in Value.WholeNumber
            select new AST.NodeYAttribute(value) as AST.NodeAttribute;

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

        public static TokenListParser<TokenKind, AST.NodeAttribute> NodeAlignColumnAttribute { get; } =
            from key in Key(NodeAttributeKind.AlignHorizontal)
            from value in AlignColumn.OrNone()
            select new AST.NodeAlignColumnAttribute(value) as AST.NodeAttribute;

        public static TokenListParser<TokenKind, AST.NodeAttribute> NodeAlignRowAttribute { get; } =
            from key in Key(NodeAttributeKind.AlignVertical)
            from value in AlignRow.OrNone()
            select new AST.NodeAlignRowAttribute(value) as AST.NodeAttribute;

        public static TokenListParser<TokenKind, AST.NodeAttribute> NodeAlignAttribute { get; } =
            from key in Key(NodeAttributeKind.Align)
            from value in AlignColumnOnly.Then(c => AlignRow.OrNone().OptionalOrDefault(default(AlignmentKind?)).Select(r => (new AlignmentKind?(c), r)))
                .Or(AlignRowOnly.Then(r => AlignColumn.OrNone().OptionalOrDefault(default(AlignmentKind?)).Select(c => (c, new AlignmentKind?(r)))))
                .Or(Identifier.Enum<AlignmentKind>().OrNone().Then(cOrBoth => AlignRow.OrNone().OptionalOrDefault(default(AlignmentKind?)).Select(rOrNeither => (cOrBoth, rOrNeither ?? cOrBoth))))
            select new AST.NodeAlignAttribute(value.Item1, value.Item2) as AST.NodeAttribute;

        public static TokenListParser<TokenKind, AST.NodeAttribute> NodeMarginAttribute { get; } =
            from key in Key(NodeAttributeKind.Margin)
            from value in Value.Border
            select new AST.NodeMarginAttribute(value) as AST.NodeAttribute;

        public static TokenListParser<TokenKind, AST.NodeAttribute> NodeCornerRadiusAttribute { get; } =
            from key in Key(NodeAttributeKind.Corner)
            from value in Value.WholeNumber
            select new AST.NodeCornerRadiusAttribute(value) as AST.NodeAttribute;

        public static TokenListParser<TokenKind, AST.NodeAttribute> NodeAttribute { get; } =
            NodeLabelContentAttribute
                .Or(NodeLabelJustifyAttribute)
                .Or(NodeLabelAttribute)
                .Or(NodeShapeAttribute)
                .Or(NodeAlignColumnAttribute)
                .Or(NodeAlignRowAttribute)
                .Or(NodeAlignAttribute)
                .Or(NodeMarginAttribute)
                .Or(NodeCornerRadiusAttribute)
                .Or(NodeColumnAttribute)
                .Or(NodeRowAttribute)
                .Or(NodeXAttribute)
                .Or(NodeYAttribute)
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

        public static TokenListParser<TokenKind, AST.RegionAttribute> RegionGridFlowAttribute { get; } =
            from key in Key(RegionAttributeKind.GridFlow)
            from value in Identifier.Enum<FlowKind>()
            select new AST.RegionGridFlowAttribute(value) as AST.RegionAttribute;

        public static TokenListParser<TokenKind, AST.RegionAttribute> RegionGridMaxAttribute { get; } =
            from key in Key(RegionAttributeKind.GridMax)
            from value in Value.CountingNumber
            select new AST.RegionGridMaxAttribute(value) as AST.RegionAttribute;

        public static TokenListParser<TokenKind, AST.RegionAttribute> RegionGridAttribute { get; } =
            from key in Key(RegionAttributeKind.Grid)
            from values in Shorthand(Identifier.Enum<FlowKind>(), Value.CountingNumber)
            select values switch { (var flow, var max) => new AST.RegionGridAttribute(flow, max) as AST.RegionAttribute };

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

        public static TokenListParser<TokenKind, AST.RegionAttribute> RegionLayoutColumnsAttribute { get; } =
            from key in Key(RegionAttributeKind.LayoutColumns)
            from value in Value.TrackSize
            select new AST.RegionLayoutColumnsAttribute(value) as AST.RegionAttribute;

        public static TokenListParser<TokenKind, AST.RegionAttribute> RegionLayoutRowsAttribute { get; } =
            from key in Key(RegionAttributeKind.LayoutRows)
            from value in Value.TrackSize
            select new AST.RegionLayoutRowsAttribute(value) as AST.RegionAttribute;

        public static TokenListParser<TokenKind, AST.RegionAttribute> RegionLayoutAttribute { get; } =
            from key in Key(RegionAttributeKind.Layout)
            from columnsAndRows in Value.TrackSize.Twice()
            select new AST.RegionLayoutAttribute(columnsAndRows.first, columnsAndRows.second) as AST.RegionAttribute;

        public static TokenListParser<TokenKind, AST.RegionAttribute> RegionJustifyColumnsAttribute { get; } =
            from key in Key(RegionAttributeKind.JustifyColumns)
            from value in AlignColumn
            select new AST.RegionJustifyColumnsAttribute(value) as AST.RegionAttribute;

        public static TokenListParser<TokenKind, AST.RegionAttribute> RegionJustifyRowsAttribute { get; } =
            from key in Key(RegionAttributeKind.JustifyRows)
            from value in AlignRow
            select new AST.RegionJustifyRowsAttribute(value) as AST.RegionAttribute;

        public static TokenListParser<TokenKind, AST.RegionAttribute> RegionJustifyAttribute { get; } =
            from key in Key(RegionAttributeKind.Justify)
            from columnsAndRows in AlignColumnOnly.Then(c => AlignRow.OptionalOrDefault(AlignmentKind.Center).Select(r => (c, r)))
                .Or(AlignRowOnly.Then(r => AlignColumn.OptionalOrDefault(AlignmentKind.Center).Select(c => (c, r))))
                .Or(Identifier.Enum<AlignmentKind>().Then(cOrBoth => AlignRow.Select(r => new AlignmentKind?(r)).OptionalOrDefault(default(AlignmentKind?)).Select(rOrNeither => (cOrBoth, rOrNeither ?? cOrBoth))))
            select new AST.RegionJustifyAttribute(columnsAndRows.Item1, columnsAndRows.Item2) as AST.RegionAttribute;

        public static TokenListParser<TokenKind, AST.RegionAttribute> RegionAttribute { get; } =
            RegionFillAttribute
                .Or(RegionPaddingAttribute)
                .Or(RegionGridFlowAttribute)
                .Or(RegionGridMaxAttribute)
                .Or(RegionGridAttribute)
                .Or(RegionSpaceColumnsAttribute)
                .Or(RegionSpaceRowsAttribute)
                .Or(RegionSpaceAttribute)
                .Or(RegionLayoutColumnsAttribute)
                .Or(RegionLayoutRowsAttribute)
                .Or(RegionLayoutAttribute)
                .Or(RegionJustifyColumnsAttribute)
                .Or(RegionJustifyRowsAttribute)
                .Or(RegionJustifyAttribute);
        #endregion
    }
}
