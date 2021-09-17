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

        public static TokenListParser<TokenKind, TA[]> AttributeList<TA>(TokenListParser<TokenKind, TA> attributeParser) =>
            from begin in Token.EqualTo(TokenKind.LeftBracket)
            from values in attributeParser.AtLeastOnceDelimitedBy(Token.EqualTo(TokenKind.Comma))
            from end in Token.EqualTo(TokenKind.RightBracket)
            select values;

        public static TokenListParser<TokenKind, AST.Node> Node { get; } =
            from keyword in Token.EqualToValue(TokenKind.Keyword, "node")
            from label in String
            from attrs in AttributeList(AttributeParsers.NodeAttribute).OptionalOrDefault(Array.Empty<AST.NodeAttribute>())            
            select new AST.Node(label, attrs);

        public static TokenListParser<TokenKind, AST.Edge> Edge { get; } =
            from keyword in Token.EqualToValue(TokenKind.Keyword, "edge")
            from @from in String
            from @to in String
            from attrs in AttributeList(AttributeParsers.EdgeAttribute).OptionalOrDefault(Array.Empty<AST.EdgeAttribute>())
            select new AST.Edge(@from, @to, attrs);

        public static TokenListParser<TokenKind, AST.Declaration?> Declaration { get; } =
            Node.Cast<AST.Node, AST.Declaration>().AsNullable()
            .Or(Edge.Cast<AST.Edge, AST.Declaration>().AsNullable())
            .OptionalOrDefault();
            
        public static TokenListParser<TokenKind, AST.Document> Document { get; } =
            Declaration.ManyDelimitedBy(NewLine)
                .Select(decs => new AST.Document(decs.WhereNotNull().ToArray()))
                .AtEnd();
    }
}
