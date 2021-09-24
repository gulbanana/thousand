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

        public static TokenListParser<TokenKind, string[]> ClassList { get; } =
            Keyword.AtLeastOnceDelimitedBy(Token.EqualTo(TokenKind.Period));

        public static TokenListParser<TokenKind, string[]> BaseClasses { get; } =
            Token.EqualTo(TokenKind.Colon)
                 .IgnoreThen(ClassList)
                 .OptionalOrDefault(Array.Empty<string>());

        public static TokenListParser<TokenKind, AST.Class> Class { get; } =
            from keyword in Token.EqualToValue(TokenKind.Keyword, "class")
            from name in Keyword
            from bases in BaseClasses
            from attrs in AttributeList(AttributeParsers.NodeAttribute).OptionalOrDefault(Array.Empty<AST.NodeAttribute>())
            select new AST.Class(name, bases, attrs);

        public static TokenListParser<TokenKind, AST.Node> Node { get; } =
            from classes in ClassList
            from identifier in Keyword.AsNullable().OptionalOrDefault()
            from label in String.AsNullable().OptionalOrDefault()
            from attrs in AttributeList(AttributeParsers.NodeAttribute).OptionalOrDefault(Array.Empty<AST.NodeAttribute>())
            from children in Superpower.Parse.Ref(() => Scope!).Select(s => s.Declarations).AsNullable().OptionalOrDefault(Array.Empty<AST.ScopeDeclaration>())
            select new AST.Node(classes, identifier, label, attrs, children);

        public static TokenListParser<TokenKind, IEnumerable<AST.Edge>> TerminalEdge { get; } =
            from dst in Target
            select Enumerable.Repeat(new AST.Edge(dst, null), 1);

        public static TokenListParser<TokenKind, IEnumerable<AST.Edge>> Edges { get; } =
            from src in Target
            from arrow in Token.EqualTo(TokenKind.RightArrow).Value(ArrowKind.Forward)
                          .Or(Token.EqualTo(TokenKind.LeftArrow).Value(ArrowKind.Backward))
            from next in Superpower.Parse.Ref(() => Edges!).Try().Or(TerminalEdge)
            select next.Prepend(new(src, arrow));

        public static TokenListParser<TokenKind, AST.Edges> AttributedEdges { get; } =
            from chain in Edges
            from attrs in AttributeList(AttributeParsers.EdgeAttribute).OptionalOrDefault(Array.Empty<AST.EdgeAttribute>())
            select new AST.Edges(chain.ToArray(), attrs);

        public static TokenListParser<TokenKind, AST.ScopeDeclaration?> ScopeDeclaration { get; } =
            AttributeParsers.NodeAttribute.Cast<AST.NodeAttribute, AST.ScopeDeclaration>()
                .Or(AttributedEdges.Cast<AST.Edges, AST.ScopeDeclaration>().Try())
                .Or(Node.Cast<AST.Node, AST.ScopeDeclaration>())
                .AsNullable()
                .OptionalOrDefault();

        public static TokenListParser<TokenKind, AST.Scope> Scope { get; } =
            from begin in Token.EqualTo(TokenKind.LeftBrace)
            from decs in ScopeDeclaration.ManyDelimitedBy(NewLine)
            from end in Token.EqualTo(TokenKind.RightBrace)
            select new AST.Scope(decs.WhereNotNull().ToArray());

        public static TokenListParser<TokenKind, AST.DocumentDeclaration?> DocumentDeclaration { get; } =
            AttributeParsers.DocumentAttribute.Cast<AST.DocumentAttribute, AST.DocumentDeclaration>()
                .Or(AttributedEdges.Cast<AST.Edges, AST.DocumentDeclaration>().Try())
                .Or(Class.Cast<AST.Class, AST.DocumentDeclaration>())
                .Or(Node.Cast<AST.Node, AST.DocumentDeclaration>())
                .AsNullable()
                .OptionalOrDefault();
            
        public static TokenListParser<TokenKind, AST.Document> Document { get; } =
            DocumentDeclaration.ManyDelimitedBy(NewLine)
                .Select(decs => new AST.Document(decs.WhereNotNull().ToArray()))
                .AtEnd();
    }
}
