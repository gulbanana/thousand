using OneOf;
using Superpower;
using Superpower.Model;
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
        public static TextSpan GetEnd(string source)
        {
            var p = Position.Zero;

            for (var i = 0; i < source.Length; ++i)
            {
                p = p.Advance(source[p.Absolute]);
            }

            return new TextSpan(source, p, 0);
        }

        /***********************
         * Class/object parts. *
         ***********************/

        public static TokenListParser<TokenKind, Name> ClassReference { get; } =
            Identifier.Any.Named("class name");

        public static TokenListParser<TokenKind, Name> ObjectReference { get; } =
            Identifier.String.Or(Identifier.Any).Named("object name");

        public static TokenListParser<TokenKind, Name[]> ClassList { get; } =
            ClassReference.AtLeastOnceDelimitedBy(Token.EqualTo(TokenKind.Period));

        public static TokenListParser<TokenKind, Name[]> BaseClasses { get; } =
            Token.EqualTo(TokenKind.Colon)
                 .IgnoreThen(ClassList)
                 .OptionalOrDefault(Array.Empty<Name>());

        /***************
         * Line parts. *
         ***************/
        public static TokenListParser<TokenKind, T> Inline<T>(TokenListParser<TokenKind, Func<IMacro<bool>, T>> pT) =>
            from begin in Token.EqualTo(TokenKind.LeftParenthesisUnbound)
            from t in pT
            from end in Macro.Of(Token.EqualTo(TokenKind.RightParenthesis).Value(true).OptionalOrDefault(false))
            select t(end);

        public static TokenListParser<TokenKind, OneOf<Name, T>> SegmentTarget<T>(TokenListParser<TokenKind, Func<IMacro<bool>, T>> pT) =>
            Inline(pT).Select(x => (OneOf<Name, T>)x)
                .Or(ObjectReference.Select(x => (OneOf<Name, T>)x));

        public static TokenListParser<TokenKind, IEnumerable<AST.LineSegment<T>>> TerminalSegment<T>(TokenListParser<TokenKind, Func<IMacro<bool>, T>> pT) =>
            from dst in SegmentTarget(pT)
            select Enumerable.Repeat(new AST.LineSegment<T>(dst, null), 1);

        public static TokenListParser<TokenKind, ArrowKind> Arrow { get; } =
            from begin in Token.EqualTo(TokenKind.LineSeparator).Optional()
            from arrow in Token.EqualTo(TokenKind.Arrow).Select(t => t.ToStringValue() switch
            {
                "->" => ArrowKind.Forward,
                "<-" => ArrowKind.Backward,
                "<>" => ArrowKind.Both,
                "--" or _ => ArrowKind.Neither
            })
            from end in Token.EqualTo(TokenKind.LineSeparator).Optional()
            select arrow;

        public static TokenListParser<TokenKind, IEnumerable<AST.LineSegment<T>>> LineSegments<T>(TokenListParser<TokenKind, Func<IMacro<bool>, T>> pT) =>
            from src in SegmentTarget(pT)
            from arrow in Arrow
            from next in Ref(() => LineSegments(pT)!).Try().Or(TerminalSegment(pT))
            select next.Prepend(new(src, arrow));
    }
}
