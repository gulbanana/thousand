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

        // extended version of ManyDelimitedBy. supports missing and/or invalid elements and produces better error messages
        public static TokenListParser<TokenKind, IReadOnlyList<T>> ManyOptionalDelimitedBy<T>(
            this TokenListParser<TokenKind, T> elementParser,
            TokenKind delimiter,
            TokenKind? terminator = null,
            Func<TokenList<TokenKind>, TokenList<TokenKind>, T>? empty = null,
            Func<TokenList<TokenKind>, TokenList<TokenKind>, T>? invalid = null) => input =>
        {
            var results = new List<T>();
            var provisionalResult = default(T);
            var hasProvisionalResult = false;

            var remainder = input;
            var element = input;
            while (true)
            {
                var next = remainder.ConsumeToken();

                // terminate/delimit: produce a result, then...
                if (!next.HasValue || next.Value.Kind == delimiter || (terminator != null && next.Value.Kind == terminator))
                {
                    if (hasProvisionalResult)
                    {
                        results.Add(provisionalResult!);
                    }
                    else if (remainder.Position == element.Position)
                    {
                        if (empty != null)
                        {
                            results.Add(empty(element, remainder));
                        }
                    }
                    else 
                    {
                        if (invalid != null)
                        {
                            results.Add(invalid(element, remainder));
                        }
                        else
                        {
                            return TokenListParserResult.Empty<TokenKind, IReadOnlyList<T>>(input);
                        }
                    }

                    // ...begin the element
                    if (next.HasValue && next.Value.Kind == delimiter)
                    {
                        hasProvisionalResult = false;
                        element = next.Remainder;
                        remainder = next.Remainder;
                    }

                    // ...return
                    else
                    {
                        break;
                    }
                }

                // trailer: invalidate the interim result (subsequent content leaves it invalid)
                else if (hasProvisionalResult)
                {
                    hasProvisionalResult = false;
                    remainder = next.Remainder;
                }

                // content: produce an interim result, so that it can consume separators
                else if (element.Position == remainder.Position)
                {
                    var result = elementParser(element);
                    if (result.HasValue)
                    {
                        provisionalResult = result.Value;
                        hasProvisionalResult = true;
                        remainder = result.Remainder;
                    }
                    else
                    {
                        remainder = next.Remainder;
                    }
                }

                // continued trailers
                else
                {
                    remainder = next.Remainder;
                }
            }

            return TokenListParserResult.Value<TokenKind, IReadOnlyList<T>>(results, input, remainder);
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
