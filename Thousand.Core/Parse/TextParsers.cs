using Superpower;
using Superpower.Model;
using Superpower.Parsers;
using System.Globalization;
using System.Linq;
using Thousand.Model;

namespace Thousand.Parse
{
    internal static class TextParsers
    {
        private static bool IsLatinDigit(char ch)
        {
            return ch >= '0' && ch <= '9';
        }

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

        /// <summary>A positive integer</summary>
        public static TextParser<int> CountingNumber { get; } = input =>
        {
            var next = input.ConsumeChar();

            if (!next.HasValue || !IsLatinDigit(next.Value))
            {
                return Result.Empty<int>(input, "digit");
            }

            TextSpan remainder;
            var val = 0;
            do
            {
                val = 10 * val + (next.Value - '0');
                remainder = next.Remainder;
                next = remainder.ConsumeChar();
            } while (next.HasValue && IsLatinDigit(next.Value));

            if (val < 1)
            {
                return Result.Empty<int>(input, "positive number");
            }

            return Result.Value(val, input, remainder);
        };

        /// <summary>A non-negative integer</summary>
        public static TextParser<int> WholeNumber { get; } = input =>
        {
            var next = input.ConsumeChar();

            if (!next.HasValue || !IsLatinDigit(next.Value))
            {
                return Result.Empty<int>(input, "digit");
            }

            TextSpan remainder;
            var val = 0;
            do
            {
                val = 10 * val + (next.Value - '0');
                remainder = next.Remainder;
                next = remainder.ConsumeChar();
            } while (next.HasValue && IsLatinDigit(next.Value));

            if (val < 0)
            {
                return Result.Empty<int>(input, "non-negative number");
            }

            return Result.Value(val, input, remainder);
        };

        /// <summary>Matches decimal numbers, for example <code>-1.23</code>, converted into a <see cref="float"/>.</summary>
        public static TextParser<float> DecimalFloat { get; } =
            Numerics.Decimal.Select(span => float.Parse(span.ToStringValue(), CultureInfo.InvariantCulture));
    }
}
