using Superpower;
using Superpower.Model;
using Superpower.Parsers;
using System.Linq;
using Thousand.Model;

namespace Thousand.Parse
{
    public static class AttributeParsers
    {
        private static TokenListParser<TokenKind, Token<TokenKind>> Key<TK>(TK kind) where TK : struct =>
            Token.EqualToValueIgnoreCase(TokenKind.Keyword, kind.ToString()!)
                 .IgnoreThen(Token.EqualTo(TokenKind.EqualsSign));

        public static TokenListParser<TokenKind, Colour> ColourValue { get; } =
            Token.EqualTo(TokenKind.Colour).Apply(TextParsers.Colour)
                .Or(Keyword.Statics<Colour>());

        public static TokenListParser<TokenKind, int> CountingNumberValue { get; } =
            Token.EqualTo(TokenKind.Number).Apply(TextParsers.CountingNumber);

        public static TokenListParser<TokenKind, float> DecimalValue { get; } =
            Token.EqualTo(TokenKind.Number).Apply(TextParsers.DecimalFloat);

        public static TokenListParser<TokenKind, AST.DocumentAttribute> DocumentScaleAttribute { get; } =
            from key in Key(DiagramAttributeKind.Scale)
            from value in DecimalValue
            select new AST.DocumentScaleAttribute(value) as AST.DocumentAttribute;

        public static TokenListParser<TokenKind, AST.DocumentAttribute> DocumentBackgroundAttribute { get; } =
            from key in Key(DiagramAttributeKind.Background)
            from value in ColourValue
            select new AST.DocumentBackgroundAttribute(value) as AST.DocumentAttribute;

        public static TokenListParser<TokenKind, AST.DocumentAttribute> DocumentAttribute { get; } =
            DocumentScaleAttribute
            .Or(DocumentBackgroundAttribute);

        public static TokenListParser<TokenKind, AST.NodeAttribute> NodeLabelAttribute { get; } =
            from key in Key(NodeAttributeKind.Label)
            from value in Token.EqualTo(TokenKind.String).Apply(TextParsers.String)
            select new AST.NodeLabelAttribute(value) as AST.NodeAttribute;

        public static TokenListParser<TokenKind, AST.NodeAttribute> NodeShapeAttribute { get; } =
            from key in Key(NodeAttributeKind.Shape)
            from value in Keyword.Enum<ShapeKind>()
            select new AST.NodeShapeAttribute(value) as AST.NodeAttribute;

        public static TokenListParser<TokenKind, AST.NodeAttribute> NodeStrokeAttribute { get; } =
            from key in Key(NodeAttributeKind.Stroke)
            from value in ColourValue
            select new AST.NodeStrokeAttribute(value) as AST.NodeAttribute;

        public static TokenListParser<TokenKind, AST.NodeAttribute> NodeFillAttribute { get; } =
            from key in Key(NodeAttributeKind.Fill)
            from value in ColourValue
            select new AST.NodeFillAttribute(value) as AST.NodeAttribute;

        public static TokenListParser<TokenKind, AST.NodeAttribute> NodeRowAttribute { get; } =
            from key in Key(NodeAttributeKind.Row)
            from value in CountingNumberValue
            select new AST.NodeRowAttribute(value) as AST.NodeAttribute;

        public static TokenListParser<TokenKind, AST.NodeAttribute> NodeColumnAttribute { get; } =
            from key in Key(NodeAttributeKind.Column)
            from value in CountingNumberValue
            select new AST.NodeColumnAttribute(value) as AST.NodeAttribute;

        public static TokenListParser<TokenKind, AST.NodeAttribute> NodeAttribute { get; } =
            NodeLabelAttribute
                .Or(NodeShapeAttribute)
                .Or(NodeStrokeAttribute)
                .Or(NodeFillAttribute)
                .Or(NodeRowAttribute)
                .Or(NodeColumnAttribute);

        public static TokenListParser<TokenKind, AST.EdgeAttribute> EdgeStrokeAttribute { get; } =
            from key in Key(EdgeAttributeKind.Stroke)
            from value in ColourValue
            select new AST.EdgeStrokeAttribute(value) as AST.EdgeAttribute;

        public static TokenListParser<TokenKind, AST.EdgeAttribute> EdgeAttribute { get; } =
            EdgeStrokeAttribute;
    }
}
