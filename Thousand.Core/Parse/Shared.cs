﻿using Superpower;
using Superpower.Parsers;
using System;
using System.Collections.Generic;
using System.Linq;
using Thousand.Model;
using static Superpower.Parse;

namespace Thousand.Parse
{
    public static class Shared
    {
        public static TokenListParser<TokenKind, TA[]> List<TA>(TokenListParser<TokenKind, TA> attributeParser) =>
            from begin in Token.EqualTo(TokenKind.LeftBracket)
            from values in attributeParser.ManyDelimitedBy(Token.EqualTo(TokenKind.Comma))
            from end in Token.EqualTo(TokenKind.RightBracket)
            select values;

        public static TokenListParser<TokenKind, T[]> Scope<T>(TokenListParser<TokenKind, T> pT) =>
            from begin in Token.EqualTo(TokenKind.LeftBrace)
            from decs in pT.ManyOptionalDelimited(terminator: TokenKind.RightBrace)
            from end in Token.EqualTo(TokenKind.RightBrace)
            select decs.ToArray();

        /*******************************
         * Subclass/object/line parts. *
         *******************************/

        public static TokenListParser<TokenKind, Identifier[]> ClassList { get; } =
            Identifier.Any.AtLeastOnceDelimitedBy(Token.EqualTo(TokenKind.Period));

        public static TokenListParser<TokenKind, Identifier[]> BaseClasses { get; } =
            Token.EqualTo(TokenKind.Colon)
                 .IgnoreThen(ClassList)
                 .OptionalOrDefault(Array.Empty<Identifier>());

        /***************
         * Line parts. *
         ***************/

        public static TokenListParser<TokenKind, Identifier> Target { get; } =
            Value.StringIdentifier.Or(Identifier.Any);

        public static TokenListParser<TokenKind, IEnumerable<AST.LineSegment>> TerminalEdge { get; } =
            from dst in Target
            select Enumerable.Repeat(new AST.LineSegment(dst, null), 1);

        public static TokenListParser<TokenKind, ArrowKind> Arrow { get; } =
            Token.EqualTo(TokenKind.RightArrow).Value(ArrowKind.Forward)
                .Or(Token.EqualTo(TokenKind.LeftArrow).Value(ArrowKind.Backward))
                .Or(Token.EqualTo(TokenKind.NoArrow).Value(ArrowKind.Neither))
                .Or(Token.EqualTo(TokenKind.DoubleArrow).Value(ArrowKind.Both));

        public static TokenListParser<TokenKind, IEnumerable<AST.LineSegment>> Edges { get; } =
            from src in Target
            from arrow in Arrow
            from next in Ref(() => Edges!).Try().Or(TerminalEdge)
            select next.Prepend(new(src, arrow));
    }
}
