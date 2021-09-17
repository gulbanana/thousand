using Superpower;
using Superpower.Parsers;
using System;
using System.Linq;

namespace Thousand
{
    public static class Enums
    {
        public static TokenListParser<TokenKind, T> Value<T>() where T : struct, Enum
        {
            var values = Enum.GetValues<T>();

            if (!values.Any())
            {
                throw new Exception($"Enum {typeof(T).Name} has no values.");
            }

            var parser = Token.EqualToValueIgnoreCase(TokenKind.Keyword, values.First().ToString()!).Value(values.First());
            foreach (var v in values.Skip(1))
            {
                parser = parser.Or(Token.EqualToValueIgnoreCase(TokenKind.Keyword, v.ToString()!).Value(v));
            }
            return parser;
        }
    }
}
