using Superpower;
using Superpower.Model;
using Superpower.Parsers;
using System.Linq;

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

        public static TokenListParser<TokenKind, AST.NodeAttribute> NodeAttribute { get; } = 
            NodeLabelAttribute
                .Or(NodeShapeAttribute);

        public static TokenListParser<TokenKind, AST.EdgeAttribute> EdgeAttribute { get; } =
            Token.EqualTo(TokenKind.Keyword).Value(default(AST.EdgeAttribute)!);
    }
}
