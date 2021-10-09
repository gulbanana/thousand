using Superpower;
using Superpower.Model;
using Superpower.Parsers;
using System.Linq;
using Thousand.Model;

namespace Thousand.Parse
{
    public static class Value
    {
        public static TokenListParser<TokenKind, Macro> Macro(params TokenKind[] terminators) => input =>
        {
            var remainder = input;
            while (!remainder.IsAtEnd)
            {
                var next = remainder.ConsumeToken();
                if (terminators.Contains(next.Value.Kind))
                {
                    break;
                }

                remainder = next.Remainder;
            }
            
            return TokenListParserResult.Value(new Macro(input, remainder), input, remainder);
        };

        public static TokenListParser<TokenKind, int> CountingNumber { get; } =
            Token.EqualTo(TokenKind.Number).Apply(TextParsers.CountingNumber);

        public static TokenListParser<TokenKind, int> WholeNumber { get; } =
            Token.EqualTo(TokenKind.Number).Apply(TextParsers.WholeNumber);

        public static TokenListParser<TokenKind, int> Integer { get; } =
            Token.EqualTo(TokenKind.Number).Apply(Numerics.IntegerInt32);

        public static TokenListParser<TokenKind, decimal> Decimal { get; } =
            Token.EqualTo(TokenKind.Number).Apply(Numerics.DecimalDecimal);

        public static TokenListParser<TokenKind, string> String { get; } =
            Token.EqualTo(TokenKind.String).Apply(TextParsers.String);

        public static TokenListParser<TokenKind, Identifier> StringIdentifier { get; } =
            Token.EqualTo(TokenKind.String).Apply(TextParsers.String.Select(s => new Identifier(s)).Located());

        public static TokenListParser<TokenKind, string?> NullableString { get; } =
            Token.EqualTo(TokenKind.String).Apply(TextParsers.String).AsNullable()
                .Or(Token.EqualTo(TokenKind.NoneKeyword).Value(default(string?)));

        public static TokenListParser<TokenKind, Anchor> Anchor { get; } =
            Token.EqualToValueIgnoreCase(TokenKind.Identifier, "any").Value(new AnyAnchor() as Anchor)
                .Or(Token.EqualToValueIgnoreCase(TokenKind.Identifier, "corners").Or(Token.EqualToValueIgnoreCase(TokenKind.Identifier, "corner")).Value(new CornerAnchor() as Anchor))
                .Or(Identifier.Enum<CompassKind>().Select(k => new SpecificAnchor(k) as Anchor))
                .OrDefault(new NoAnchor());

        public static TokenListParser<TokenKind, Border> Border { get; } =
            from first in WholeNumber
            from second in WholeNumber.Optional()
            from thirdAndFourth in WholeNumber.Then(a => WholeNumber.Select(b => (third: a, fourth: b))).Optional()
            select thirdAndFourth.HasValue ? new Border(first, second.Value, thirdAndFourth.Value.third, thirdAndFourth.Value.fourth) :
                   second.HasValue ? new Border(first, second.Value) :
                   new Border(first);

        public static TokenListParser<TokenKind, Colour> Colour { get; } =
            Token.EqualTo(TokenKind.Colour).Apply(TextParsers.Colour)
                .Or(Identifier.Statics<Colour>());

        public static TokenListParser<TokenKind, TrackSize> TrackSize { get; } =
            Token.EqualToValueIgnoreCase(TokenKind.Identifier, "pack").Value(new PackedSize() as TrackSize)
                .Or(Token.EqualToValueIgnoreCase(TokenKind.Identifier, "equal").Value(new EqualSize() as TrackSize))
                .Or(WholeNumber.Select(x => new MinimumSize(x) as TrackSize));

        public static TokenListParser<TokenKind, Width> Width { get; } =
            Token.EqualToValueIgnoreCase(TokenKind.Identifier, "hairline").Value(new HairlineWidth() as Width)
                .Or(WholeNumber.OrNone().Select(x => x.HasValue ? new PositiveWidth(x.Value) : new ZeroWidth() as Width));
    }
}
