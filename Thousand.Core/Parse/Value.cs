using Superpower;
using Superpower.Parsers;
using System.Linq;
using Thousand.Model;

namespace Thousand.Parse
{
    public static class Value
    {
        public static TokenListParser<TokenKind, int> CountingNumber { get; } =
            Token.EqualTo(TokenKind.Number).Apply(TextParsers.CountingNumber).Named("positive number");

        public static TokenListParser<TokenKind, int> WholeNumber { get; } =
            Token.EqualTo(TokenKind.Number).Apply(TextParsers.WholeNumber).Named("non-negative number");

        public static TokenListParser<TokenKind, int> Integer { get; } =
            Token.EqualTo(TokenKind.Number).Apply(Numerics.IntegerInt32).Named("number");

        // XXX these can produce bad errors ("unsatisfied condition") and should instead use textparsers
        public static TokenListParser<TokenKind, decimal> CountingDecimal { get; } =
            Token.EqualTo(TokenKind.Number).Apply(Numerics.DecimalDecimal).Where(d => d > 0).Named("positive decimal");

        public static TokenListParser<TokenKind, decimal> WholeDecimal { get; } =
            Token.EqualTo(TokenKind.Number).Apply(Numerics.DecimalDecimal).Where(d => d >= 0).Named("non-negative decimal");

        public static TokenListParser<TokenKind, string> String { get; } =
            Token.EqualTo(TokenKind.String).Apply(TextParsers.String);

        public static TokenListParser<TokenKind, string> Identifier { get; } =
            String.Or(Token.EqualTo(TokenKind.Identifier).Apply(TextParsers.Identifier));

        public static TokenListParser<TokenKind, string?> NullableString { get; } =
            Token.EqualTo(TokenKind.String).Apply(TextParsers.String).AsNullable()
                .Or(Token.EqualTo(TokenKind.NoneKeyword).Value(default(string?)));

        public static TokenListParser<TokenKind, Text> Text { get; } =
            NullableString
                .Or(Token.EqualTo(TokenKind.Identifier).Apply(TextParsers.Identifier).AsNullable())
                .Select(t => new Text(t));

        public static TokenListParser<TokenKind, AlignmentKind> AlignColumnOnly { get; } =
            Token.EqualToValueIgnoreCase(TokenKind.Identifier, "left").Value(AlignmentKind.Start)
                .Or(Token.EqualToValueIgnoreCase(TokenKind.Identifier, "right").Value(AlignmentKind.End));

        public static TokenListParser<TokenKind, AlignmentKind> AlignColumn { get; } =
            AlignColumnOnly.Or(Parse.Identifier.Enum<AlignmentKind>());

        public static TokenListParser<TokenKind, AlignmentKind> AlignRowOnly { get; } =
            Token.EqualToValueIgnoreCase(TokenKind.Identifier, "top").Value(AlignmentKind.Start)
                .Or(Token.EqualToValueIgnoreCase(TokenKind.Identifier, "bottom").Value(AlignmentKind.End));

        public static TokenListParser<TokenKind, AlignmentKind> AlignRow { get; } =
            AlignRowOnly.Or(Parse.Identifier.Enum<AlignmentKind>());

        public static TokenListParser<TokenKind, Anchor> Anchor { get; } =
            Token.EqualToValueIgnoreCase(TokenKind.Identifier, "any").Value(new AnyAnchor() as Anchor)
                .Or(Token.EqualToValueIgnoreCase(TokenKind.Identifier, "corners").Or(Token.EqualToValueIgnoreCase(TokenKind.Identifier, "corner")).Value(new CornerAnchor() as Anchor))
                .Or(Parse.Identifier.Enum<CompassKind>().Select(k => new SpecificAnchor(k) as Anchor))
                .OrDefault(new NoAnchor());

        public static TokenListParser<TokenKind, Border> Border { get; } =
            from first in WholeDecimal
            from second in WholeDecimal.Optional()
            from thirdAndFourth in WholeDecimal.Then(a => WholeDecimal.Select(b => (third: a, fourth: b))).Optional()
            select thirdAndFourth.HasValue ? new Border(first, second.Value, thirdAndFourth.Value.third, thirdAndFourth.Value.fourth) :
                   second.HasValue ? new Border(first, second.Value) :
                   new Border(first);

        public static TokenListParser<TokenKind, Colour> Colour { get; } =
            Token.EqualTo(TokenKind.Colour).Apply(TextParsers.Colour)
                .Or(Parse.Identifier.Statics<Colour>())
                .Named("colour");

        public static TokenListParser<TokenKind, Point> Point { get; } =
            (from x in Integer
             from y in Integer
             select new Point(x, y)).Named("point");

        public static TokenListParser<TokenKind, Width> Width { get; } =
            WholeNumber.Select(x => new PositiveWidth(x) as Width)
                .Or(Token.EqualToValueIgnoreCase(TokenKind.Identifier, "hairline").Value(HairlineWidth.Instance))
                .OrDefault(NoWidth.Instance);
    }
}
