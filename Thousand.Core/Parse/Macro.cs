using Superpower;
using Superpower.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Thousand.Parse
{
    public record Macro(TokenList<TokenKind> Location, TokenList<TokenKind> Remainder) : IMacro
    {
        public static TokenListParser<TokenKind, IMacro> Empty() => input =>
        {
            return TokenListParserResult.Value<TokenKind, IMacro>(new Macro(input, input), input, input);
        };

        public static TokenListParser<TokenKind, IMacro<T>> Empty<T>(T defaultValue) => input =>
        {
            return TokenListParserResult.Value<TokenKind, IMacro<T>>(new Macro<T>(input, input, defaultValue), input, input);
        };

        public static TokenListParser<TokenKind, IMacro> Raw(params TokenKind[] terminators) => input =>
        {
            var remainder = input;
            while (!remainder.IsAtEnd)
            {
                var next = remainder.ConsumeToken();
                if (terminators.Contains(next.Value.Kind))
                {
                    break;
                }

                remainder = next.Remainder;
            }

            return TokenListParserResult.Value<TokenKind, IMacro>(new Macro(input, remainder), input, remainder);
        };

        public static TokenListParser<TokenKind, IMacro<T>> Of<T>(TokenListParser<TokenKind, T> pT) => input =>
        {
            var t = pT(input);
            if (t.HasValue)
            {
                return TokenListParserResult.Value<TokenKind, IMacro<T>>(new Macro<T>(input, t.Remainder, t.Value), input, t.Remainder);
            }
            else
            {
                return TokenListParserResult.CastEmpty<TokenKind, T, IMacro<T>>(t);
            }
        };

        public IEnumerable<Token<TokenKind>> Sequence()
        {
            return Location.Take(Remainder.Position - Location.Position);
        }

        public Range Range(int offset = 0)
        {
            var start = Location.Position - offset;
            var end = Remainder.Position - offset;
            return start..end;
        }

        public TextSpan SpanOrEmpty() => Span(TextSpan.Empty);

        public TextSpan Span(TextSpan endSpan)
        {
            if (Location.IsAtEnd)
            {
                return endSpan;
            }

            var first = Location.First().Span;
            if (first.Source == null)
            {
                throw new InvalidOperationException("macro location token span has no source");
            }

            if (Remainder.IsAtEnd)
            {
                return new TextSpan(first.Source, first.Position, first.Source.Length - first.Position.Absolute);
            }
            else
            {
                var tokenDiff = Remainder.Position - Location.Position;
                if (tokenDiff == 0)
                {
                    return new TextSpan(first.Source, first.Position, 0);
                }
                else
                {
                    var last = Location.ElementAt(tokenDiff - 1).Span;
                    return new TextSpan(first.Source, first.Position, last.Position.Absolute - first.Position.Absolute + last.Length);
                }
            }
        }
    }

    public record Macro<T>(TokenList<TokenKind> Location, TokenList<TokenKind> Remainder, T Value) : Macro(Location, Remainder), IMacro<T>
    {
        public IMacro<U> Select<U>(Func<T, U> f)
        {
            return new Macro<U>(Location, Remainder, f(Value));
        }
    }
}
