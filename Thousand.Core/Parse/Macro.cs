using Superpower;
using Superpower.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using Thousand.AST;

namespace Thousand.Parse
{
    // XXX basically ITemplated with concrete init - extract the interface somehow
    public record Macro(TokenList<TokenKind> Location, TokenList<TokenKind> Remainder)
    {
        public static TokenListParser<TokenKind, Macro> Empty { get; } = input =>
        {
            return TokenListParserResult.Value(new Macro(input, input), input, input);
        };

        public static TokenListParser<TokenKind, Macro> Raw(params TokenKind[] terminators) => input =>
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

            return TokenListParserResult.Value(new Macro(input, remainder), input, remainder);
        };

        public static TokenListParser<TokenKind, Macro<T>> Of<T>(TokenListParser<TokenKind, T> pT) => input =>
        {            
            var t = pT(input);
            if (t.HasValue)
            {
                return TokenListParserResult.Value(new Macro<T>(input, t.Remainder, t.Value), input, t.Remainder);
            }
            else
            {
                return TokenListParserResult.CastEmpty<TokenKind, T, Macro<T>>(t);
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

        public TextSpan Span()
        {
            if (Location.IsAtEnd)
            {
                return TextSpan.Empty;
            }

            var first = Location.First().Span;
            if (first.Source == null)
            {
                return TextSpan.Empty;
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

    public record Macro<T>(TokenList<TokenKind> Location, TokenList<TokenKind> Remainder, T Value) : Macro(Location, Remainder)
    {
        public Macro<U> Select<U>(Func<T, U> f)
        {
            return new Macro<U>(Location, Remainder, f(Value));
        }
    }
}
