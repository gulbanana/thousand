using Superpower;
using Superpower.Model;
using Superpower.Parsers;
using System.Collections.Generic;

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

        public static TokenListParser<TokenKind, T?> OrNone<T>(this TokenListParser<TokenKind, T> pT) where T : struct
        {
            return pT
                .Select(v => new T?(v))
                .Or(Token.EqualTo(TokenKind.NoneKeyword).Value(new T?()));
        }

        // produces better error messages than ManyDelimitedBy by making some assumptions
        public static TokenListParser<TokenKind, IReadOnlyList<T>> ManyOptionalDelimited<T>(this TokenListParser<TokenKind, T> parser, TokenKind? terminator = null) => originalInput =>
        {
            var results = new List<T>();

            var input = originalInput;
            while (!input.IsAtEnd)
            {
                var first = input.ConsumeToken();
                if (!first.HasValue) return TokenListParserResult.Empty<TokenKind, IReadOnlyList<T>>(input, "!IsAtEnd, but no token present");

                if (terminator != null && first.Value.Kind == terminator.Value)
                {
                    break;
                }
                else if (first.Value.Kind == TokenKind.LineSeparator)
                {
                    input = first.Remainder;
                }
                else
                {
                    var result = parser(input);
                    if (result.HasValue)
                    {
                        results.Add(result.Value);
                        input = result.Remainder;
                    }
                    else
                    {
                        return TokenListParserResult.CastEmpty<TokenKind, T, IReadOnlyList<T>>(result);
                    }
                }
            }

            return TokenListParserResult.Value<TokenKind, IReadOnlyList<T>>(results, originalInput, input);
        };
    }
}
