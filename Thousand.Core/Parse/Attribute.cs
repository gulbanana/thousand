using Superpower;
using Superpower.Model;
using Superpower.Parsers;
using System.Collections.Generic;
using System.Linq;
using Thousand.Model;

namespace Thousand.Parse
{
    internal static class Attribute
    {
        public static TokenListParser<TokenKind, (T1?, T2?)> ShorthandVV<T1, T2>(TokenListParser<TokenKind, T1> p1, TokenListParser<TokenKind, T2> p2)
            where T1 : struct
            where T2 : struct
        => (TokenList<TokenKind> input) =>
        {
            var x1 = default(T1?);
            var x2 = default(T2?);

            var remainder = input;
            while (!remainder.IsAtEnd && (!x1.HasValue || !x2.HasValue))
            {
                var next = remainder.ConsumeToken();

                var asT1 = p1.Try()(remainder);
                var asT2 = p2.Try()(remainder);

                if (asT1.HasValue)
                {
                    x1 = asT1.Value;
                }
                else if (asT2.HasValue)
                {
                    x2 = asT2.Value;
                }
                else
                {
                    break;
                }

                remainder = next.Remainder;
            }

            if (x1.HasValue || x2.HasValue)
            {
                return TokenListParserResult.Value((x1, x2), input, remainder);
            }
            else
            {
                var singleResult = p1.Value(Unit.Value).Or(p2.Value(Unit.Value)).Or(p2.Value(Unit.Value))(input);
                return TokenListParserResult.CastEmpty<TokenKind, Unit, (T1?, T2?)>(singleResult);
            }
        };

        public static TokenListParser<TokenKind, (T1?, T2?, T3?)> ShorthandRRV<T1, T2, T3>(TokenListParser<TokenKind, T1> p1, TokenListParser<TokenKind, T2> p2, TokenListParser<TokenKind, T3> p3)
            where T1 : class
            where T2 : class
            where T3 : struct
        => (TokenList<TokenKind> input) =>
        {
            var x1 = default(T1?);
            var x2 = default(T2?);
            var x3 = default(T3?);

            var remainder = input;
            while (!remainder.IsAtEnd && (x1 == null || x2 == null || !x3.HasValue))
            {
                var next = remainder.ConsumeToken();

                var asT1 = p1.Try()(remainder);
                var asT2 = p2.Try()(remainder);
                var asT3 = p3.Try()(remainder);

                if (asT1.HasValue)
                {
                    x1 = asT1.Value;
                }
                else if (asT2.HasValue)
                {
                    x2 = asT2.Value;
                }
                else if (asT3.HasValue)
                {
                    x3 = asT3.Value;
                }
                else
                {
                    break;
                }

                remainder = next.Remainder;
            }

            if (x1 != null || x2 != null || x3.HasValue)
            {
                return TokenListParserResult.Value((x1, x2, x3), input, remainder);
            }
            else
            {
                var singleResult = p1.Value(Unit.Value).Or(p2.Value(Unit.Value)).Or(p3.Value(Unit.Value))(input);
                return TokenListParserResult.CastEmpty<TokenKind, Unit, (T1?, T2?, T3?)>(singleResult);
            }
        };

        public static TokenListParser<TokenKind, (T1?, T2?, T3?)> ShorthandRVV<T1, T2, T3>(TokenListParser<TokenKind, T1> p1, TokenListParser<TokenKind, T2> p2, TokenListParser<TokenKind, T3> p3)
            where T1 : class
            where T2 : struct
            where T3 : struct
        => (TokenList<TokenKind> input) =>
        {
            var x1 = default(T1?);
            var x2 = default(T2?);
            var x3 = default(T3?);

            var remainder = input;
            while (!remainder.IsAtEnd && (x1 == null || x2 == null || !x3.HasValue))
            {
                var next = remainder.ConsumeToken();

                var asT1 = p1.Try()(remainder);
                var asT2 = p2.Try()(remainder);
                var asT3 = p3.Try()(remainder);

                if (asT1.HasValue)
                {
                    x1 = asT1.Value;
                }
                else if (asT2.HasValue)
                {
                    x2 = asT2.Value;
                }
                else if (asT3.HasValue)
                {
                    x3 = asT3.Value;
                }
                else
                {
                    break;
                }

                remainder = next.Remainder;
            }

            if (x1 != null || x2 != null || x3.HasValue)
            {
                return TokenListParserResult.Value((x1, x2, x3), input, remainder);
            }
            else
            {
                var singleResult = p1.Value(Unit.Value).Or(p2.Value(Unit.Value)).Or(p3.Value(Unit.Value))(input);
                return TokenListParserResult.CastEmpty<TokenKind, Unit, (T1?, T2?, T3?)>(singleResult);
            }
        };

        public static TokenListParser<TokenKind, T> From<T>(API.AttributeDefinition<T> definition)
        {
            var keys = definition.Names.Select(name => Token.EqualToValueIgnoreCase(TokenKind.Identifier, name)).Aggregate((left, right) => left.Or(right));

            return from key in keys
                   from _ in Token.EqualTo(TokenKind.EqualsSign)
                   from value in definition.ValueParser
                   select value;
        }

        public static TokenListParser<TokenKind, T> From<T>(IEnumerable<API.AttributeDefinition<T>> definitions)
        {
            return definitions.Select(From).Aggregate((left, right) => left.Or(right));
        }
    }
}
