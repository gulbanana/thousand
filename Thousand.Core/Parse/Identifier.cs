using Superpower;
using Superpower.Model;
using Superpower.Parsers;
using System;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Thousand.Parse
{
    static class Identifier
    {
        private static readonly Regex uncameller = new("(?<first>[A-Z])(?<second>[A-Z])(?<lower>[a-z])|(?<lEnd>[a-z])(?<uStart>[A-Z])", RegexOptions.Compiled);

        private static string UnCamel(string val)
        {
            // convert lowerCamelCase to UpperCamelCase
            if (val.Length > 0)
            {
                val = (val[0..1].ToUpper() + val[1..]).Trim();
            }

            // "AAAa" -> "AA-Aa", "aA" -> "a-A"
            return uncameller.Replace(val, "${first}${lEnd}-${second}${lower}${uStart}");
        }

        private static TextParser<Model.Name> Wrap(TextParser<string> parser)
        {
            return input => {
                var inner = parser(input);
                if (!inner.HasValue) return Result.CastEmpty<string, Model.Name>(inner);
                return Result.Value(new Model.Name(inner.Value, inner.Location.Until(inner.Remainder)), inner.Location, inner.Remainder);
            };
        }

        public static TokenListParser<TokenKind, Model.Name> Any { get; } =
            Token.EqualTo(TokenKind.Identifier).Apply(Wrap(TextParsers.Identifier));

        public static TokenListParser<TokenKind, Model.Name> Variable { get; } =
            Token.EqualTo(TokenKind.Variable).Apply(Wrap(Character.EqualTo('$').IgnoreThen(TextParsers.Identifier)));

        public static TokenListParser<TokenKind, Model.Name> String { get; } =
            Token.EqualTo(TokenKind.String).Apply(Wrap(TextParsers.String));

        public static TokenListParser<TokenKind, T> Enum<T>() where T : struct, Enum
        {
            var names = System.Enum.GetNames<T>();
            if (!names.Any())
            {
                throw new Exception($"enum `{typeof(T).Name}` has no values");
            }

            var namedValues = names
                .Select(n => UnCamel(n).ToLowerInvariant())
                .Zip(names.Select(System.Enum.Parse<T>), Tuple.Create);

            var parser = Token.EqualToValueIgnoreCase(TokenKind.Identifier, namedValues.First().Item1).Value(System.Enum.Parse<T>(names.First()));
            foreach (var t in namedValues.Skip(1))
            {
                parser = parser.Or(Token.EqualToValueIgnoreCase(TokenKind.Identifier, t.Item1).Value(t.Item2));
            }
            return parser;
        }

        public static TokenListParser<TokenKind, T> EnumValue<T>(T value) where T : struct, Enum
        {
            var names = System.Enum.GetNames<T>()
                .Where(n => System.Enum.Parse<T>(n).Equals(value))
                .Select(n => UnCamel(n).ToLowerInvariant());

            var parser = Token.EqualToValueIgnoreCase(TokenKind.Identifier, names.First());
            foreach (var n in names.Skip(1))
            {
                parser = parser.Or(Token.EqualToValueIgnoreCase(TokenKind.Identifier, n));
            }
            return parser.Value(value);
        }

        public static TokenListParser<TokenKind, T> Statics<T>()
        {
            var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Static);
            if (!props.Any())
            {
                throw new Exception($"type `{typeof(T).Name}` has no public statics");
            }

            var names = props.Select(p => p.Name);            
            var values = props.Select(p => (T)p.GetValue(null)!);
            var namedValues = names
                .Select(n => UnCamel(n).ToLowerInvariant())
                .Zip(values, Tuple.Create);

            var parser = Token.EqualToValueIgnoreCase(TokenKind.Identifier, namedValues.First().Item1).Value(values.First());
            foreach (var t in namedValues.Skip(1))
            {
                parser = parser.Or(Token.EqualToValueIgnoreCase(TokenKind.Identifier, t.Item1).Value(t.Item2));
            }
            return parser;
        }

    }
}
