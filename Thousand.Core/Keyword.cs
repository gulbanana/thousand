using Superpower;
using Superpower.Parsers;
using System;
using System.Linq;
using System.Reflection;

namespace Thousand
{
    public static class Keyword
    {
        public static TokenListParser<TokenKind, T> Enum<T>() where T : struct, Enum
        {
            var values = System.Enum.GetValues<T>();
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

        public static TokenListParser<TokenKind, T> Statics<T>()
        {
            var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Static);
            if (!props.Any())
            {
                throw new Exception($"Type {typeof(T).Name} has no public statics.");
            }

            var names = props.Select(p => p.Name);            
            var values = props.Select(p => (T)p.GetValue(null)!);

            var parser = Token.EqualToValueIgnoreCase(TokenKind.Keyword, names.First()).Value(values.First());
            foreach (var t in names.Zip(values, Tuple.Create).Skip(1))
            {
                parser = parser.Or(Token.EqualToValueIgnoreCase(TokenKind.Keyword, t.Item1).Value(t.Item2));
            }
            return parser;
        }
    }
}
