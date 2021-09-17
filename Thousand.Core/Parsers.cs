using Superpower;
using Superpower.Model;
using Superpower.Parsers;
using System;
using System.Linq;

namespace Thousand
{
    public static class Parsers
    {
        public static TokenListParser<TokenKind, Unit> NewLine { get; } =
            Token.EqualTo(TokenKind.NewLine).Value(Unit.Value);

        public static TokenListParser<TokenKind, string> String { get; } =
            Token.EqualTo(TokenKind.String).Apply(TextParsers.String);

        public static TokenListParser<TokenKind, AST.Attribute> Attribute { get; } =
            from k in Token.EqualTo(TokenKind.Keyword)
            from _ in Token.EqualTo(TokenKind.EqualsSign)
            from v in Token.EqualTo(TokenKind.Keyword)
            select new AST.Attribute(k.ToStringValue(), v.ToStringValue());

        public static TokenListParser<TokenKind, AST.Attribute[]> AttributeList { get; } =
            from begin in Token.EqualTo(TokenKind.LeftBracket)
            from values in Attribute.AtLeastOnceDelimitedBy(Token.EqualTo(TokenKind.Comma))
            from end in Token.EqualTo(TokenKind.RightBracket)
            select values;

        public static TokenListParser<TokenKind, AST.Node> Node { get; } =
            from _ in Token.EqualToValue(TokenKind.Keyword, "node")
            from attrs in AttributeList.OptionalOrDefault(Array.Empty<AST.Attribute>())
            from s in String
            select new AST.Node(s, attrs);

        public static TokenListParser<TokenKind, AST.Node?> Declaration { get; } =
            Node.AsNullable().OptionalOrDefault();

        public static TokenListParser<TokenKind, AST.Document> Document { get; } =
            Declaration.ManyDelimitedBy(NewLine)
                .Select(ns => new AST.Document(ns.WhereNotNull().ToArray()))
                .AtEnd();
    }
}
