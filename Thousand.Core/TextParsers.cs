using Superpower;
using Superpower.Parsers;
using System;
using System.Globalization;
using System.Linq;
using Thousand.Model;

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

        public static TextParser<Colour> Colour { get; } =
            from hash in Character.EqualTo('#')
            from r in Character.HexDigit.Repeat(2)
            from g in Character.HexDigit.Repeat(2)
            from b in Character.HexDigit.Repeat(2)
            select new Colour(byte.Parse(r, NumberStyles.HexNumber), byte.Parse(g, NumberStyles.HexNumber), byte.Parse(b, NumberStyles.HexNumber));
    }
}
