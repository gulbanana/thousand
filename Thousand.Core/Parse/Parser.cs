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

        public static TokenListParser<TokenKind, AST.ObjectAttribute> ObjectAttribute { get; } =
            AttributeParsers.NodeAttribute.Select(x => (AST.ObjectAttribute)x)
                .Or(AttributeParsers.RegionAttribute.Select(x => (AST.ObjectAttribute)x))
                .Or(AttributeParsers.LineAttribute.Select(x => (AST.ObjectAttribute)x))
                .Or(AttributeParsers.TextAttribute.Select(x => (AST.ObjectAttribute)x));


        public static TokenListParser<TokenKind, AST.ObjectDeclaration?> ObjectDeclaration { get; } =
            ObjectAttribute.Select(a => (AST.ObjectDeclaration)a)
                .Or(Superpower.Parse.Ref(() => AttributedEdges!).Select(a => (AST.ObjectDeclaration)a).Try())
                .Or(Superpower.Parse.Ref(() => Object!).Select(a => (AST.ObjectDeclaration)a))
                .AsNullable()
                .OptionalOrDefault();

        public static TokenListParser<TokenKind, AST.ObjectDeclaration[]> Scope { get; } =
            from begin in Token.EqualTo(TokenKind.LeftBrace)
            from decs in ObjectDeclaration.ManyDelimitedBy(NewLine)
            from end in Token.EqualTo(TokenKind.RightBrace)
            select decs.WhereNotNull().ToArray();

        public static TokenListParser<TokenKind, AST.Class> Class { get; } =
            from keyword in Token.EqualToValue(TokenKind.Keyword, "class")
            from name in Keyword
            from bases in BaseClasses
            from attrs in AttributeList(ObjectAttribute).OptionalOrDefault(Array.Empty<AST.ObjectAttribute>())
            select new AST.Class(name, bases, attrs);

        public static TokenListParser<TokenKind, AST.TypedObject> Object { get; } =
            from classes in ClassList
            from name in Keyword.Or(String).AsNullable().OptionalOrDefault()
            from attrs in AttributeList(ObjectAttribute).OptionalOrDefault(Array.Empty<AST.ObjectAttribute>())
            from children in Superpower.Parse.Ref(() => Scope!).OptionalOrDefault(Array.Empty<AST.ObjectDeclaration>())
            select new AST.TypedObject(classes, name, attrs, children);

        public static TokenListParser<TokenKind, IEnumerable<AST.Edge>> TerminalEdge { get; } =
            from dst in Target
            select Enumerable.Repeat(new AST.Edge(dst, null), 1);

        public static TokenListParser<TokenKind, IEnumerable<AST.Edge>> Edges { get; } =
            from src in Target
            from arrow in Token.EqualTo(TokenKind.RightArrow).Value(ArrowKind.Forward)
                          .Or(Token.EqualTo(TokenKind.LeftArrow).Value(ArrowKind.Backward))
            from next in Superpower.Parse.Ref(() => Edges!).Try().Or(TerminalEdge)
            select next.Prepend(new(src, arrow));

        public static TokenListParser<TokenKind, AST.EdgeAttribute> EdgeAttribute { get; } =
            AttributeParsers.ArrowAttribute.Select(x => (AST.EdgeAttribute)x)
                .Or(AttributeParsers.LineAttribute.Select(x => (AST.EdgeAttribute)x));

        public static TokenListParser<TokenKind, AST.EdgeChain> AttributedEdges { get; } =
            from chain in Edges
            from attrs in AttributeList(EdgeAttribute).OptionalOrDefault(Array.Empty<AST.EdgeAttribute>())
            select new AST.EdgeChain(chain.ToArray(), attrs);

        public static TokenListParser<TokenKind, AST.DiagramAttribute> DiagramAttribute { get; } =
            AttributeParsers.DocumentAttribute.Select(x => (AST.DiagramAttribute)x)
                .Or(AttributeParsers.RegionAttribute.Select(x => (AST.DiagramAttribute)x));

        public static TokenListParser<TokenKind, AST.DocumentDeclaration?> DocumentDeclaration { get; } =
            DiagramAttribute.Select(x => (AST.DocumentDeclaration)x)
                .Or(AttributedEdges.Select(x => (AST.DocumentDeclaration)x).Try())
                .Or(Class.Select(x => (AST.DocumentDeclaration)x))
                .Or(Object.Select(x => (AST.DocumentDeclaration)x))
                .AsNullable()
                .OptionalOrDefault();
            
        public static TokenListParser<TokenKind, AST.Document> Document { get; } =
            DocumentDeclaration.ManyDelimitedBy(NewLine)
                .Select(decs => new AST.Document(decs.WhereNotNull().ToArray()))
                .AtEnd();
    }
}
