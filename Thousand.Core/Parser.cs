using Superpower;
using Superpower.Model;
using Superpower.Parsers;
using System;
using System.Linq;

namespace Thousand
{
    public static class Parser
    {
        public static TokenListParser<TokenKind, AST.Document> Build()
        {
            return Parser.Document;
        }

        public static TokenListParser<TokenKind, Unit> NewLine { get; } =
            Token.EqualTo(TokenKind.NewLine).Value(Unit.Value);

        public static TokenListParser<TokenKind, string> String { get; } =
            Token.EqualTo(TokenKind.String).Apply(TextParsers.String);

        public static TokenListParser<TokenKind, AST.NodeAttribute[]> AttributeList { get; } =
            from begin in Token.EqualTo(TokenKind.LeftBracket)
            from values in AttributeParsers.NodeAttribute.AtLeastOnceDelimitedBy(Token.EqualTo(TokenKind.Comma))
            from end in Token.EqualTo(TokenKind.RightBracket)
            select values;

        public static TokenListParser<TokenKind, AST.Node> Node { get; } =
            from keyword in Token.EqualToValue(TokenKind.Keyword, "node")
            from label in String
            from attrs in AttributeList.OptionalOrDefault(Array.Empty<AST.NodeAttribute>())            
            select new AST.Node(label, attrs);

        public static TokenListParser<TokenKind, AST.Node?> Declaration { get; } =
            Node.AsNullable().OptionalOrDefault();

        public static TokenListParser<TokenKind, AST.Document> Document { get; } =
            Declaration.ManyDelimitedBy(NewLine)
                .Select(ns => new AST.Document(ns.WhereNotNull().ToArray()))
                .AtEnd();
    }
}
