using Superpower;
using Superpower.Model;
using Superpower.Parsers;
using System.Linq;
using Thousand.Model;

namespace Thousand
{
    public static class AttributeParsers
    {
        private static TokenListParser<TokenKind, Token<TokenKind>> Key<TK>(TK kind) where TK : struct =>
            Token.EqualToValueIgnoreCase(TokenKind.Keyword, kind.ToString()!)
                 .IgnoreThen(Token.EqualTo(TokenKind.EqualsSign));

        public static TokenListParser<TokenKind, AST.NodeAttribute> NodeLabelAttribute { get; } =
            from key in Key(NodeAttributeKind.Label)
            from value in Token.EqualTo(TokenKind.String).Apply(TextParsers.String)
            select new AST.NodeLabelAttribute(value) as AST.NodeAttribute;

        public static TokenListParser<TokenKind, AST.NodeAttribute> NodeShapeAttribute { get; } =
            from key in Key(NodeAttributeKind.Shape)
            from shape in Enums.Value<ShapeKind>()
            select new AST.NodeShapeAttribute(shape) as AST.NodeAttribute;

        public static TokenListParser<TokenKind, AST.NodeAttribute> NodeStrokeAttribute { get; } =
            from key in Key(NodeAttributeKind.Stroke)
            from value in Token.EqualTo(TokenKind.Colour).Apply(TextParsers.Colour)
            select new AST.NodeStrokeAttribute(value) as AST.NodeAttribute;

        public static TokenListParser<TokenKind, AST.NodeAttribute> NodeFillAttribute { get; } =
            from key in Key(NodeAttributeKind.Fill)
            from value in Token.EqualTo(TokenKind.Colour).Apply(TextParsers.Colour)
            select new AST.NodeFillAttribute(value) as AST.NodeAttribute;

        public static TokenListParser<TokenKind, AST.NodeAttribute> NodeAttribute { get; } =
            NodeLabelAttribute
                .Or(NodeShapeAttribute)
                .Or(NodeStrokeAttribute)
                .Or(NodeFillAttribute);

        public static TokenListParser<TokenKind, AST.EdgeAttribute> EdgeStrokeAttribute { get; } =
            from key in Key(EdgeAttributeKind.Stroke)
            from value in Token.EqualTo(TokenKind.Colour).Apply(TextParsers.Colour)
            select new AST.EdgeStrokeAttribute(value) as AST.EdgeAttribute;

        public static TokenListParser<TokenKind, AST.EdgeAttribute> EdgeAttribute { get; } =
            EdgeStrokeAttribute;
    }
}
