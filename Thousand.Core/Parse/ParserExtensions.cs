using Superpower;
using Superpower.Model;
using Superpower.Parsers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Thousand.Parse
{
    internal static class ParserExtensions
    {
        public static TokenListParser<TokenKind, U> Cast<T, U>(this TokenListParser<TokenKind, T> pT) where T : U
        {
            return pT.Select(x => (U)x);
        }

        public static TokenListParser<TokenKind, T?> OrNull<T>(this TokenListParser<TokenKind, T> pT) where T : class
        {
            return pT
                .AsNullable()
                .Or(Token.EqualTo(TokenKind.NoneKeyword).Value(default(T?)));
        }

        public static TokenListParser<TokenKind, T> OrDefault<T>(this TokenListParser<TokenKind, T> pT, T defaultValue) where T : class
        {
            return pT
                .Or(Token.EqualTo(TokenKind.NoneKeyword).Value(defaultValue));
        }

        public static TokenListParser<TokenKind, T?> OrNone<T>(this TokenListParser<TokenKind, T> pT) where T : struct
        {
            return pT
                .Select(v => new T?(v))
                .Or(Token.EqualTo(TokenKind.NoneKeyword).Value(new T?()));
        }

        public static TokenListParser<TokenKind, (T first, T second)> Twice<T>(this TokenListParser<TokenKind, T> pT)
        {
            return from first in pT
                   from firstAndSecond in pT.Select(second => (first, second)).OptionalOrDefault((first, first))
                   select firstAndSecond;
        }

        // produces better error messages than ManyDelimitedBy by making some assumptions
        public static TokenListParser<TokenKind, IReadOnlyList<T>> ManyOptionalDelimited<T>(
            this TokenListParser<TokenKind, T> parser, 
            TokenKind? terminator = null,
            Func<TokenList<TokenKind>, TokenList<TokenKind>, T>? invalid = null,
            Func<TokenList<TokenKind>, TokenList<TokenKind>, T>? fallback = null) => originalInput =>
        {
            var results = new List<T>();
            var provisionalResult = default((T result, TokenList<TokenKind> input)?);

            var input = originalInput;
            var beganLine = default(TokenList<TokenKind>?);
            while (!input.IsAtEnd)
            {
                var first = input.ConsumeToken();
                if (!first.HasValue) return TokenListParserResult.Empty<TokenKind, IReadOnlyList<T>>(input, "!IsAtEnd, but no token present");

                // found an alternate terminator
                if (terminator != null && first.Value.Kind == terminator.Value)
                {
                    if (provisionalResult.HasValue)
                    {
                        results.Add(provisionalResult.Value.result);
                        provisionalResult = null;
                    }
                    else if (fallback != null && beganLine.HasValue)
                    {
                        results.Add(fallback(beganLine.Value, input));
                    }

                    beganLine = input;
                    break;
                }

                // found a primary terminator
                else if (first.Value.Kind == TokenKind.LineSeparator)
                {
                    if (provisionalResult.HasValue)
                    {
                        results.Add(provisionalResult.Value.result);
                        provisionalResult = null;
                    }
                    else if (fallback != null && beganLine.HasValue)
                    {
                        results.Add(fallback(beganLine.Value, input));
                    }

                    beganLine = input;
                    input = first.Remainder;                    
                }

                // found a trailer - nonterminal tokens after a parsed result. add an invalid result instead and skip to the next line
                else if (invalid != null && provisionalResult.HasValue)
                {
                    var remainder = provisionalResult.Value.input;
                    while (!remainder.IsAtEnd && remainder.First().Kind != TokenKind.LineSeparator && (!terminator.HasValue || remainder.First().Kind != terminator.Value))
                    {
                        remainder = remainder.ConsumeToken().Remainder;
                    }

                    results.Add(invalid(provisionalResult.Value.input, remainder));
                    provisionalResult = null;
                    input = remainder;
                }

                // attempt to find a result
                else
                {
                    var result = parser(input);
                    if (result.HasValue)
                    {
                        if (invalid != null)
                        {
                            provisionalResult = (result.Value, input);
                        }
                        else
                        {
                            results.Add(result.Value);
                        }

                        if (input.Position == result.Remainder.Position)
                        {
                            break;
                        }
                        else
                        {
                            input = result.Remainder;
                        }
                    }
                    else
                    {
                        return TokenListParserResult.CastEmpty<TokenKind, T, IReadOnlyList<T>>(result);
                    }
                }
            }

            if (provisionalResult.HasValue)
            {
                results.Add(provisionalResult.Value.result);
            }
            else if (fallback != null && beganLine.HasValue)
            {
                results.Add(fallback(beganLine.Value, input));
            }

            return TokenListParserResult.Value<TokenKind, IReadOnlyList<T>>(results, originalInput, input);
        };

        public static TextParser<T> Located<T>(this TextParser<T> parser) where T : ILocated
        {
            return input => {
                var inner = parser(input);
                if (!inner.HasValue) return inner;
                inner.Value.Span = inner.Location.Until(inner.Remainder);
                return inner;
            };
        }

        public static string Dump(this IEnumerable<Token<TokenKind>> list)
        {
            return string.Join(" ", list.Select(t => t.ToStringValue()));
        }
    }
}
