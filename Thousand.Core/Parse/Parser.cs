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

        // XXX rewrite this to work like DocumentDeclaration when i have a more-fixed idea of what's in an object scope
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

        public static TokenListParser<TokenKind, AST.TypedObject> ObjectContent(string[] classes) =>
            from name in Identifier.Or(String).AsNullable().OptionalOrDefault()
            from attrs in AttributeList(ObjectAttribute).OptionalOrDefault(Array.Empty<AST.ObjectAttribute>())
            from children in Superpower.Parse.Ref(() => Scope!).OptionalOrDefault(Array.Empty<AST.ObjectDeclaration>())
            select new AST.TypedObject(classes, name, attrs, children);

        public static TokenListParser<TokenKind, AST.TypedObject> Object { get; } =
            from classes in ClassList
            from objekt in ObjectContent(classes)
            select objekt;

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

        public static TokenListParser<TokenKind, AST.TypedLine> LineContent(string[] classes) =>
            from chain in Edges
            from attrs in AttributeList(EdgeAttribute).OptionalOrDefault(Array.Empty<AST.LineAttribute>())
            select new AST.TypedLine(classes, chain.ToArray(), attrs);

        public static TokenListParser<TokenKind, AST.TypedLine> Line { get; } =
            from classes in ClassList
            from line in LineContent(classes)
            select line;

        public static TokenListParser<TokenKind, AST.DiagramAttribute> DiagramAttribute { get; } =
            AttributeParsers.DocumentAttribute.Select(x => (AST.DiagramAttribute)x)
                .Or(AttributeParsers.RegionAttribute.Select(x => (AST.DiagramAttribute)x));

        public static TokenListParser<TokenKind, AST.Class> ObjectClassBody(string name, string[] bases) =>
            AttributeList(ObjectAttribute).OptionalOrDefault(Array.Empty<AST.ObjectAttribute>()).Select(attrs => new AST.ObjectClass(name, bases, attrs) as AST.Class);

        public static TokenListParser<TokenKind, AST.Class> LineClassBody(string name, string[] bases) =>
            AttributeList(EdgeAttribute).OptionalOrDefault(Array.Empty<AST.LineAttribute>()).Select(attrs => new AST.LineClass(name, bases, attrs) as AST.Class);

        public static TokenListParser<TokenKind, AST.Class> ObjectOrLineClassBody(string name, string[] bases) =>
            AttributeList(AttributeParsers.LineAttribute).OptionalOrDefault(Array.Empty<AST.StrokeAttribute>()).Select(attrs => new AST.ObjectOrLineClass(name, bases, attrs) as AST.Class);

        public static TokenListParser<TokenKind, AST.Class> ClassContent { get; } =
            from name in Identifier
            from bases in BaseClasses
            from klass in ObjectOrLineClassBody(name, bases).Try().Or(ObjectClassBody(name, bases).Try()).Or(LineClassBody(name, bases))
            select klass;

        public static TokenListParser<TokenKind, AST.Class> Class { get; } =
            from keyword in Token.EqualTo(TokenKind.ClassKeyword)
            from klass in ClassContent
            select klass;

        // handwritten top-level parser, because the language syntax is pretty ambiguous. if parsed with combinators, the errors aren't very good
        public static TokenListParser<TokenKind, IReadOnlyList<AST.DocumentDeclaration>> DocumentDeclarations { get; } = originalInput =>
        {
            var result = new List<AST.DocumentDeclaration>();

            var input = originalInput;
            while (!input.IsAtEnd)
            {
                var fail = TokenListParserResult.Empty<TokenKind, IReadOnlyList<AST.DocumentDeclaration>>(input, new[] { "attribute", "class", "object", "line" });

                var first = input.ConsumeToken();
                if (!first.HasValue) return fail;

                if (first.Value.Kind == TokenKind.NewLine)
                {
                    input = first.Remainder;
                }
                else if (first.Value.Kind == TokenKind.ClassKeyword) // could be a class declaration
                {
                    var klass = ClassContent(first.Remainder);
                    if (klass.HasValue)
                    {
                        result.Add(klass.Value);
                        input = klass.Remainder;
                    }
                    else
                    {
                        return TokenListParserResult.CastEmpty<TokenKind, AST.Class, IReadOnlyList<AST.DocumentDeclaration>>(klass);                        
                    }
                }
                else if (first.Value.Kind == TokenKind.Identifier) // could be an attribute, an object or a line
                {
                    var second = first.Remainder.ConsumeToken();
                    if (!second.HasValue) return fail;

                    if (second.Value.Kind == TokenKind.EqualsSign) // can only be an attribute
                    {
                        var attr = DiagramAttribute.Select(x => (AST.DocumentDeclaration)x)(input);
                        if (attr.HasValue)
                        {
                            result.Add(attr.Value);
                            input = attr.Remainder;
                        }
                        else
                        {
                            return TokenListParserResult.CastEmpty<TokenKind, AST.DocumentDeclaration, IReadOnlyList<AST.DocumentDeclaration>>(attr);
                        }
                    }
                    else // could still be an object or a line
                    {
                        var classList = ClassList(input);
                        if (classList.HasValue)
                        {
                            var identifier = classList.Remainder.ConsumeToken(); // object declaration or first object of line
                            if (!identifier.HasValue)
                            {
                                return fail;
                            }

                            var arrow = identifier.Remainder.ConsumeToken();
                            if (arrow.HasValue && arrow.Value.Kind is TokenKind.LeftArrow or TokenKind.RightArrow or TokenKind.NoArrow or TokenKind.DoubleArrow)
                            {
                                var line = LineContent(classList.Value)(classList.Remainder);
                                if (line.HasValue)
                                {
                                    result.Add(line.Value);
                                    input = line.Remainder;
                                }
                                else
                                {
                                    return TokenListParserResult.CastEmpty<TokenKind, AST.TypedLine, IReadOnlyList<AST.DocumentDeclaration>>(line);
                                }
                            }
                            else
                            {
                                var objekt = ObjectContent(classList.Value)(classList.Remainder);
                                if (objekt.HasValue)
                                {
                                    result.Add(objekt.Value);
                                    input = objekt.Remainder;
                                }
                                else
                                {
                                    return TokenListParserResult.CastEmpty<TokenKind, AST.TypedObject, IReadOnlyList<AST.DocumentDeclaration>>(objekt);
                                }
                            }
                        }
                        else
                        {
                            return fail;
                        }
                    }
                }
                else
                {
                    return fail;
                }
            }

            return TokenListParserResult.Value<TokenKind, IReadOnlyList<AST.DocumentDeclaration>>(result, originalInput, input);
        };

        public static TokenListParser<TokenKind, AST.Document> Document { get; } =
            DocumentDeclarations
                .Select(decs => new AST.Document(decs.ToArray()))
                .AtEnd();
    }
}
