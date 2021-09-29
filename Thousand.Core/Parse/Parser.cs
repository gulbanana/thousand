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

        public static TokenListParser<TokenKind, string> Identifier { get; } =
            Token.EqualTo(TokenKind.Identifier).Apply(Superpower.Parsers.Identifier.CStyle).Select(s => s.ToStringValue());

        public static TokenListParser<TokenKind, string> Target { get; } =
            String.Or(Identifier);

        public static TokenListParser<TokenKind, TA[]> AttributeList<TA>(TokenListParser<TokenKind, TA> attributeParser) =>
            from begin in Token.EqualTo(TokenKind.LeftBracket)
            from values in attributeParser.AtLeastOnceDelimitedBy(Token.EqualTo(TokenKind.Comma))
            from end in Token.EqualTo(TokenKind.RightBracket)
            select values;

        public static TokenListParser<TokenKind, string[]> ClassList { get; } =
            Identifier.AtLeastOnceDelimitedBy(Token.EqualTo(TokenKind.Period));

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
                .Or(Superpower.Parse.Ref(() => Line!).Select(a => (AST.ObjectDeclaration)a).Try())
                .Or(Superpower.Parse.Ref(() => Object!).Select(a => (AST.ObjectDeclaration)a))
                .AsNullable()
                .OptionalOrDefault();

        public static TokenListParser<TokenKind, AST.ObjectDeclaration[]> Scope { get; } =
            from begin in Token.EqualTo(TokenKind.LeftBrace)
            from decs in ObjectDeclaration.ManyDelimitedBy(NewLine)
            from end in Token.EqualTo(TokenKind.RightBrace)
            select decs.WhereNotNull().ToArray();

        public static TokenListParser<TokenKind, AST.TypedObject> Object { get; } =
            from classes in ClassList
            from name in Identifier.Or(String).AsNullable().OptionalOrDefault()
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

        public static TokenListParser<TokenKind, AST.LineAttribute> EdgeAttribute { get; } =
            AttributeParsers.ArrowAttribute.Select(x => (AST.LineAttribute)x)
                .Or(AttributeParsers.LineAttribute.Select(x => (AST.LineAttribute)x));

        public static TokenListParser<TokenKind, AST.TypedLine> Line { get; } =
            from chain in Edges
            from attrs in AttributeList(EdgeAttribute).OptionalOrDefault(Array.Empty<AST.LineAttribute>())
            select new AST.TypedLine(chain.ToArray(), attrs);

        public static TokenListParser<TokenKind, AST.DiagramAttribute> DiagramAttribute { get; } =
            AttributeParsers.DocumentAttribute.Select(x => (AST.DiagramAttribute)x)
                .Or(AttributeParsers.RegionAttribute.Select(x => (AST.DiagramAttribute)x));

        public static TokenListParser<TokenKind, AST.Class> ObjectClassBody(string name, string[] bases) =>
            AttributeList(ObjectAttribute).OptionalOrDefault(Array.Empty<AST.ObjectAttribute>()).Select(attrs => new AST.ObjectClass(name, bases, attrs) as AST.Class);

        public static TokenListParser<TokenKind, AST.Class> LineClassBody(string name, string[] bases) =>
            AttributeList(EdgeAttribute).OptionalOrDefault(Array.Empty<AST.LineAttribute>()).Select(attrs => new AST.LineClass(name, bases, attrs) as AST.Class);

        public static TokenListParser<TokenKind, AST.Class> ObjectOrLineClassBody(string name, string[] bases) =>
            AttributeList(AttributeParsers.LineAttribute).OptionalOrDefault(Array.Empty<AST.StrokeAttribute>()).Select(attrs => new AST.ObjectOrLineClass(name, bases, attrs) as AST.Class);

        public static TokenListParser<TokenKind, AST.Class> Class { get; } =
            from keyword in Token.EqualTo(TokenKind.ClassKeyword)
            from name in Identifier
            from bases in BaseClasses
            from klass in ObjectOrLineClassBody(name, bases).Try().Or(ObjectClassBody(name, bases).Try()).Or(LineClassBody(name, bases))
            select klass;

        public static TokenListParser<TokenKind, AST.DocumentDeclaration?> DocumentDeclaration { get; } =
            DiagramAttribute.Select(x => (AST.DocumentDeclaration)x)
                .Or(Line.Select(x => (AST.DocumentDeclaration)x).Try())
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
