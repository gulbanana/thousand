using OneOf;
using Superpower;
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

        public static TokenListParser<TokenKind, Identifier> ObjectName { get; } =
            Value.StringIdentifier.Or(Identifier.Any);

        public static TokenListParser<TokenKind, Identifier[]> ClassList { get; } =
            Identifier.Any.AtLeastOnceDelimitedBy(Token.EqualTo(TokenKind.Period));

        public static TokenListParser<TokenKind, Identifier[]> BaseClasses { get; } =
            Token.EqualTo(TokenKind.Colon)
                 .IgnoreThen(ClassList)
                 .OptionalOrDefault(Array.Empty<Identifier>());

        /***************
         * Line parts. *
         ***************/
        public static TokenListParser<TokenKind, T> Inline<T>(TokenListParser<TokenKind, T> pT) =>
            from begin in Token.EqualTo(TokenKind.Pipe)
            from t in pT
            from end in Token.EqualTo(TokenKind.Pipe)
            select t;

        public static TokenListParser<TokenKind, OneOf<Identifier, T>> SegmentTarget<T>(TokenListParser<TokenKind, T> pT) =>
            Inline(pT).Select(x => (OneOf<Identifier, T>)x)
                .Or(ObjectName.Select(x => (OneOf<Identifier, T>)x));

        public static TokenListParser<TokenKind, IEnumerable<AST.LineSegment<T>>> TerminalSegment<T>(TokenListParser<TokenKind, T> pT) =>
            from dst in SegmentTarget(pT)
            select Enumerable.Repeat(new AST.LineSegment<T>(dst, null), 1);

        public static TokenListParser<TokenKind, ArrowKind> Arrow { get; } =
            //from begin in Token.EqualTo(TokenKind.LineSeparator).Optional()
            from arrow in Token.EqualTo(TokenKind.RightArrow).Value(ArrowKind.Forward)
                .Or(Token.EqualTo(TokenKind.LeftArrow).Value(ArrowKind.Backward))
                .Or(Token.EqualTo(TokenKind.NoArrow).Value(ArrowKind.Neither))
                .Or(Token.EqualTo(TokenKind.DoubleArrow).Value(ArrowKind.Both))
            //from end in Token.EqualTo(TokenKind.LineSeparator).Optional()
            select arrow;

        public static TokenListParser<TokenKind, IEnumerable<AST.LineSegment<T>>> LineSegments<T>(TokenListParser<TokenKind, T> pT) =>
            from src in SegmentTarget(pT)
            from arrow in Arrow
            from next in Ref(() => LineSegments(pT)!).Try().Or(TerminalSegment(pT))
            select next.Prepend(new(src, arrow));
    }
}
