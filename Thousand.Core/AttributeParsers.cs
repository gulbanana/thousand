using Superpower;
using Superpower.Parsers;
using System.Linq;

namespace Thousand
{
    public static class AttributeParsers
    {
        public static TokenListParser<TokenKind, Superpower.Model.Token<TokenKind>> Key<TK>(TK kind, TokenKind token) where TK : struct =>
            Token.EqualToValueIgnoreCase(TokenKind.Keyword, kind.ToString()!)
                 .IgnoreThen(Token.EqualTo(TokenKind.EqualsSign)
                                  .IgnoreThen(Token.EqualTo(token)));

        public static TokenListParser<TokenKind, AST.NodeAttribute> NodeLabelAttribute { get; } =
            Key(NodeAttributeKind.Label, TokenKind.String)
                .Apply(TextParsers.String.Select(s => new AST.NodeLabelAttribute(s)))
                .Select(n => (AST.NodeAttribute)n);

        public static TokenListParser<TokenKind, AST.NodeAttribute> NodeShapeAttribute { get; } = 
            Key(NodeAttributeKind.Shape, TokenKind.Keyword)
                .Apply(TextParsers.Enum<ShapeKind>().Select(k => new AST.NodeShapeAttribute(k)))
                .Select(n => (AST.NodeAttribute)n);

        public static TokenListParser<TokenKind, AST.NodeAttribute> NodeAttribute { get; } = 
            NodeLabelAttribute
                .Or(NodeShapeAttribute);

        public static TokenListParser<TokenKind, AST.NodeAttribute[]> AttributeList { get; } =
            from begin in Token.EqualTo(TokenKind.LeftBracket)
            from values in NodeAttribute.AtLeastOnceDelimitedBy(Token.EqualTo(TokenKind.Comma))
            from end in Token.EqualTo(TokenKind.RightBracket)
            select values;
    }
}
