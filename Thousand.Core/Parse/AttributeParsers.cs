﻿using Superpower;
using Superpower.Model;
using Superpower.Parsers;
using System.Linq;
using Thousand.Model;

namespace Thousand.Parse
{
    public static class AttributeParsers
    {
        #region utilities and combinators
        private static TokenListParser<TokenKind, Token<TokenKind>> Key<TK>(TK kind) where TK : struct =>
            Token.EqualToValueIgnoreCase(TokenKind.Identifier, kind.ToString()!)
                 .IgnoreThen(Token.EqualTo(TokenKind.EqualsSign));

        private static TokenListParser<TokenKind, Token<TokenKind>> Keys<TK>(params TK[] kinds) where TK : struct =>
            kinds.Select(k => Token.EqualToValueIgnoreCase(TokenKind.Identifier, k.ToString()!))
                 .Aggregate((p1, p2) => p1.Or(p2))
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
                return TokenListParserResult.Empty<TokenKind, (T1?, T2?, T3?)>(input);
            }
        };
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
            from start in Identifier.Enum<AnchorKind>().OrNone()
            from startAndEnd in Identifier.Enum<AnchorKind>().OrNone().Select(end => (start, end)).OptionalOrDefault((start, start))
            select new AST.ArrowAnchorAttribute(startAndEnd.Item1, startAndEnd.Item2) as AST.ArrowAttribute;

        public static TokenListParser<TokenKind, AST.ArrowAttribute> ArrowOffsetStartXAttribute { get; } =
            from key in Key(ArrowAttributeKind.OffsetStartX)
            from value in Value.Integer
            select new AST.ArrowOffsetStartXAttribute(value) as AST.ArrowAttribute;

        public static TokenListParser<TokenKind, AST.ArrowAttribute> ArrowOffsetStartYAttribute { get; } =
            from key in Key(ArrowAttributeKind.OffsetStartY)
            from value in Value.Integer
            select new AST.ArrowOffsetStartYAttribute(value) as AST.ArrowAttribute;

        public static TokenListParser<TokenKind, AST.ArrowAttribute> ArrowOffsetEndXAttribute { get; } =
            from key in Key(ArrowAttributeKind.OffsetEndX)
            from value in Value.Integer
            select new AST.ArrowOffsetEndXAttribute(value) as AST.ArrowAttribute;

        public static TokenListParser<TokenKind, AST.ArrowAttribute> ArrowOffsetEndYAttribute { get; } =
            from key in Key(ArrowAttributeKind.OffsetEndY)
            from value in Value.Integer
            select new AST.ArrowOffsetEndYAttribute(value) as AST.ArrowAttribute;

        public static TokenListParser<TokenKind, AST.ArrowAttribute> ArrowOffsetXAttribute { get; } =
            from key in Key(ArrowAttributeKind.OffsetX)
            from start in Value.Integer
            from startAndEnd in Value.Integer.Select(end => (start, end)).OptionalOrDefault((start, start))
            select new AST.ArrowOffsetXAttribute(startAndEnd.Item1, startAndEnd.Item2) as AST.ArrowAttribute;

        public static TokenListParser<TokenKind, AST.ArrowAttribute> ArrowOffsetYAttribute { get; } =
            from key in Key(ArrowAttributeKind.OffsetY)
            from start in Value.Integer
            from startAndEnd in Value.Integer.Select(end => (start, end)).OptionalOrDefault((start, start))
            select new AST.ArrowOffsetYAttribute(startAndEnd.Item1, startAndEnd.Item2) as AST.ArrowAttribute;

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
        public static TokenListParser<TokenKind, AST.TextAttribute> TextLabelAttribute { get; } =
            from key in Key(TextAttributeKind.Label)
            from value in Value.NullableString
            select new AST.TextLabelAttribute(value) as AST.TextAttribute;

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
            TextLabelAttribute
                .Or(TextFontAttribute)
                .Or(TextFontFamilyAttribute)
                .Or(TextFontSizeAttribute)
                .Or(TextFontColourAttribute);
        #endregion

        #region node group, used only by objects
        public static TokenListParser<TokenKind, AST.NodeAttribute> NodeRowAttribute { get; } =
            from key in Key(NodeAttributeKind.Row)
            from value in Value.CountingNumber
            select new AST.NodeRowAttribute(value) as AST.NodeAttribute;

        public static TokenListParser<TokenKind, AST.NodeAttribute> NodeColumnAttribute { get; } =
            from key in Keys(NodeAttributeKind.Col, NodeAttributeKind.Column)
            from value in Value.CountingNumber
            select new AST.NodeColumnAttribute(value) as AST.NodeAttribute;

        public static TokenListParser<TokenKind, AST.NodeAttribute> NodeWidthAttribute { get; } =
            from key in Key(NodeAttributeKind.Width)
            from value in Value.CountingNumber
            select new AST.NodeWidthAttribute(value) as AST.NodeAttribute;

        public static TokenListParser<TokenKind, AST.NodeAttribute> NodeHeightAttribute { get; } =
            from key in Key(NodeAttributeKind.Height)
            from value in Value.CountingNumber
            select new AST.NodeHeightAttribute(value) as AST.NodeAttribute;

        public static TokenListParser<TokenKind, AST.NodeAttribute> NodeShapeAttribute { get; } =
            from key in Key(NodeAttributeKind.Shape)
            from value in Identifier.Enum<ShapeKind>().OrNone()
            select new AST.NodeShapeAttribute(value) as AST.NodeAttribute;

        public static TokenListParser<TokenKind, AST.NodeAttribute> NodePaddingAttribute { get; } =
            from key in Key(NodeAttributeKind.Padding)
            from value in Value.WholeNumber
            select new AST.NodePaddingAttribute(value) as AST.NodeAttribute;

        public static TokenListParser<TokenKind, AST.NodeAttribute> NodeCornerRadiusAttribute { get; } =
            from key in Keys(NodeAttributeKind.Corner, NodeAttributeKind.CornerRadius)
            from value in Value.WholeNumber
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
            from value in Value.Colour.OrNull()
            select new AST.RegionFillAttribute(value) as AST.RegionAttribute;

        public static TokenListParser<TokenKind, AST.RegionAttribute> RegionLayoutAttribute { get; } =
            from key in Key(RegionAttributeKind.Layout)
            from value in Identifier.Enum<LayoutKind>()
            select new AST.RegionLayoutAttribute(value) as AST.RegionAttribute;

        public static TokenListParser<TokenKind, AST.RegionAttribute> RegionMarginAttribute { get; } =
            from key in Key(RegionAttributeKind.Margin)
            from value in Value.WholeNumber
            select new AST.RegionMarginAttribute(value) as AST.RegionAttribute;

        public static TokenListParser<TokenKind, AST.RegionAttribute> RegionGutterAttribute { get; } =
            from key in Key(RegionAttributeKind.Gutter)
            from value in Value.WholeNumber
            select new AST.RegionGutterAttribute(value) as AST.RegionAttribute;

        public static TokenListParser<TokenKind, AST.RegionAttribute> RegionAttribute { get; } =
            RegionFillAttribute
                .Or(RegionLayoutAttribute)
                .Or(RegionMarginAttribute)
                .Or(RegionGutterAttribute);
        #endregion
    }
}
