using Superpower;
using Superpower.Parsers;
using System.Linq;
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

        public static TokenListParser<TokenKind, decimal> PositiveDecimal { get; } =
            Token.EqualTo(TokenKind.Number).Apply(Numerics.DecimalDecimal).Where(d => d > 0);

        public static TokenListParser<TokenKind, string> String { get; } =
            Token.EqualTo(TokenKind.String).Apply(TextParsers.String);

        public static TokenListParser<TokenKind, Identifier> StringIdentifier { get; } =
            Token.EqualTo(TokenKind.String).Apply(TextParsers.String.Select(s => new Identifier(s)).Located());

        public static TokenListParser<TokenKind, string?> NullableString { get; } =
            Token.EqualTo(TokenKind.String).Apply(TextParsers.String).AsNullable()
                .Or(Token.EqualTo(TokenKind.NoneKeyword).Value(default(string?)));

        public static TokenListParser<TokenKind, Text> Text { get; } =
            NullableString
                .Or(Token.EqualTo(TokenKind.Identifier).Apply(TextParsers.Identifier.Select(i => i.Text)).AsNullable())
                .Select(t => new Text(t));

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
                .Or(Identifier.Statics<Colour>())
                .Named("colour");

        public static TokenListParser<TokenKind, Point> Point { get; } =
            from x in Integer
            from y in Integer
            select new Point(x, y);

        public static TokenListParser<TokenKind, TrackSize> TrackSize { get; } =
            Token.EqualToValueIgnoreCase(TokenKind.Identifier, "pack").Value(new PackedSize() as TrackSize)
                .Or(Token.EqualToValueIgnoreCase(TokenKind.Identifier, "equal-area").Value(new EqualAreaSize() as TrackSize))
                .Or(Token.EqualToValueIgnoreCase(TokenKind.Identifier, "area").Value(new EqualAreaSize() as TrackSize))
                .Or(Token.EqualToValueIgnoreCase(TokenKind.Identifier, "equal-content").Value(new EqualContentSize() as TrackSize))
                .Or(Token.EqualToValueIgnoreCase(TokenKind.Identifier, "content").Value(new EqualContentSize() as TrackSize))
                .Or(WholeNumber.Select(x => new MinimumSize(x) as TrackSize));

        public static TokenListParser<TokenKind, Width> Width { get; } =
            Token.EqualToValueIgnoreCase(TokenKind.Identifier, "hairline").Value(new HairlineWidth() as Width)
                .Or(WholeNumber.OrNone().Select(x => x.HasValue ? new PositiveWidth(x.Value) : new ZeroWidth() as Width));
    }
}
