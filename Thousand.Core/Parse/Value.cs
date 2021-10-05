using Superpower;
using Superpower.Parsers;
using Thousand.Model;

namespace Thousand.Parse
{
    public static class Value
    {
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

        public static TokenListParser<TokenKind, string?> NullableString { get; } =
            Token.EqualTo(TokenKind.String).Apply(TextParsers.String).AsNullable()
                .Or(Token.EqualTo(TokenKind.NoneKeyword).Value(default(string?)));

        public static TokenListParser<TokenKind, Colour> Colour { get; } =
            Token.EqualTo(TokenKind.Colour).Apply(TextParsers.Colour)
                .Or(Identifier.Statics<Colour>());

        public static TokenListParser<TokenKind, Width> Width { get; } =
            Token.EqualToValueIgnoreCase(TokenKind.Identifier, "hairline").Value(new HairlineWidth() as Width)
                .Or(WholeNumber.OrNone().Select(x => x.HasValue ? new PositiveWidth(x.Value) : new ZeroWidth() as Width));

        public static TokenListParser<TokenKind, TrackSize> TrackSize { get; } =
            Token.EqualToValueIgnoreCase(TokenKind.Identifier, "pack").Value(new PackedSize() as TrackSize)
                .Or(Token.EqualToValueIgnoreCase(TokenKind.Identifier, "equal").Value(new EqualSize() as TrackSize))
                .Or(WholeNumber.Select(x => new FixedSize(x) as TrackSize));
    }
}
