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

        private static TextParser<Colour> LongColour { get; } =
            from r in Character.HexDigit.Repeat(2)
            from g in Character.HexDigit.Repeat(2)
            from b in Character.HexDigit.Repeat(2)
            select new Colour(byte.Parse(r, NumberStyles.HexNumber), byte.Parse(g, NumberStyles.HexNumber), byte.Parse(b, NumberStyles.HexNumber));

        private static TextParser<Colour> ShortColour { get; } =
            from r in Character.HexDigit
            from g in Character.HexDigit
            from b in Character.HexDigit
            select new Colour(
                byte.Parse(new string(new[] { r, r }), NumberStyles.HexNumber), 
                byte.Parse(new string(new[] { g, g }), NumberStyles.HexNumber), 
                byte.Parse(new string(new[] { b, b }), NumberStyles.HexNumber));

        public static TextParser<Colour> Colour { get; } =
            Character.EqualTo('#').IgnoreThen(LongColour.Try().Or(ShortColour));
    }
}
