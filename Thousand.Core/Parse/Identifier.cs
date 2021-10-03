using Superpower;
using Superpower.Parsers;
using System;
using System.Linq;
using System.Reflection;

namespace Thousand.Parse
{
    public static class Identifier
    {
        public static TokenListParser<TokenKind, string> Any { get; } =
            Token.EqualTo(TokenKind.Identifier).Apply(Superpower.Parsers.Identifier.CStyle).Select(s => s.ToStringValue());

        public static TokenListParser<TokenKind, T> Enum<T>() where T : struct, Enum
        {
            var names = System.Enum.GetNames<T>();
            if (!names.Any())
            {
                throw new Exception($"Enum {typeof(T).Name} has no values.");
            }

            var parser = Token.EqualToValueIgnoreCase(TokenKind.Identifier, names.First()).Value(System.Enum.Parse<T>(names.First()));
            foreach (var n in names.Skip(1))
            {
                parser = parser.Or(Token.EqualToValueIgnoreCase(TokenKind.Identifier, n).Value(System.Enum.Parse<T>(n)));
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

            var parser = Token.EqualToValueIgnoreCase(TokenKind.Identifier, names.First()).Value(values.First());
            foreach (var t in names.Zip(values, Tuple.Create).Skip(1))
            {
                parser = parser.Or(Token.EqualToValueIgnoreCase(TokenKind.Identifier, t.Item1).Value(t.Item2));
            }
            return parser;
        }
    }
}
