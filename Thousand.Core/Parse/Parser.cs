using Superpower;
using Superpower.Model;
using Superpower.Parsers;
using System;
using System.Collections.Generic;
using System.Linq;
using Thousand.Model;

namespace Thousand.Parse
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

        public static TokenListParser<TokenKind, string> Keyword { get; } =
            Token.EqualTo(TokenKind.Keyword).Apply(Identifier.CStyle).Select(s => s.ToStringValue());

        public static TokenListParser<TokenKind, string> Target { get; } =
            String.Or(Keyword);

        public static TokenListParser<TokenKind, TA[]> AttributeList<TA>(TokenListParser<TokenKind, TA> attributeParser) =>
            from begin in Token.EqualTo(TokenKind.LeftBracket)
            from values in attributeParser.AtLeastOnceDelimitedBy(Token.EqualTo(TokenKind.Comma))
            from end in Token.EqualTo(TokenKind.RightBracket)
            select values;

        public static TokenListParser<TokenKind, AST.Node> Node { get; } =
            from @class in Keyword
            where @class == "object"
            from identifier in Keyword.AsNullable().OptionalOrDefault()
            from label in String.AsNullable().OptionalOrDefault()
            from attrs in AttributeList(AttributeParsers.NodeAttribute).OptionalOrDefault(Array.Empty<AST.NodeAttribute>())
            select new AST.Node(@class, identifier, label, attrs);

        public static TokenListParser<TokenKind, IEnumerable<AST.Edge>> TerminalEdge { get; } =
            from dst in Target
            select Enumerable.Repeat(new AST.Edge(dst, null), 1);

        public static TokenListParser<TokenKind, IEnumerable<AST.Edge>> Edges { get; } =
            from src in Target
            from arrow in Token.EqualTo(TokenKind.Arrow)
            from next in Superpower.Parse.Ref(() => Edges!).Try().Or(TerminalEdge)
            select next.Prepend(new(src, ArrowKind.Forward));

        public static TokenListParser<TokenKind, AST.Edges> AttributedEdges { get; } =
            from chain in Edges
            from attrs in AttributeList(AttributeParsers.EdgeAttribute).OptionalOrDefault(Array.Empty<AST.EdgeAttribute>())
            select new AST.Edges(chain.ToArray(), attrs);

        public static TokenListParser<TokenKind, AST.Declaration?> Declaration { get; } =
            Node.Cast<AST.Node, AST.Declaration>().AsNullable()
            .Or(AttributedEdges.Cast<AST.Edges, AST.Declaration>().AsNullable().Try())
            .Or(AttributeParsers.DocumentAttribute.Cast<AST.DocumentAttribute, AST.Declaration>().AsNullable())
            .OptionalOrDefault();
            
        public static TokenListParser<TokenKind, AST.Document> Document { get; } =
            Declaration.ManyDelimitedBy(NewLine)
                .Select(decs => new AST.Document(decs.WhereNotNull().ToArray()))
                .AtEnd();
    }
}
