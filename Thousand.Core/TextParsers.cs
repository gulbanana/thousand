using Superpower;
using Superpower.Parsers;
using System;
using System.Linq;

namespace Thousand
{
    internal static class TextParsers
    {
        public static TextParser<string> String { get; } =
            from open in Character.EqualTo('"')
            from chars in Character.ExceptIn('"', '\\')
                .Or(Character.EqualTo('\\')
                    .IgnoreThen(
                        Character.EqualTo('\\')
                        .Or(Character.EqualTo('"'))
                        .Or(Character.EqualTo('/'))
                        .Or(Character.EqualTo('r').Value('\r'))
                        .Or(Character.EqualTo('n').Value('\n'))
                        .Or(Character.EqualTo('u').IgnoreThen(
                            Span.MatchedBy(Character.HexDigit.Repeat(4))
                                .Apply(Numerics.HexDigitsUInt32)
                                .Select(cc => (char)cc)))
                            .Named("escape sequence")))
                .Many()
            from close in Character.EqualTo('"')
            select new string(chars);

        public static TextParser<T> Enum<T>() where T : struct, Enum
        {
            var values = System.Enum.GetValues<T>();
            var parser = Span.EqualToIgnoreCase(values.First().ToString()!).Value(values.First());
            foreach (var v in values.Skip(1))
            {
                parser = parser.Or(Span.EqualToIgnoreCase(v.ToString()!).Value(v));
            }
            return parser;
        }
    }
}
