using Superpower;
using Superpower.Parsers;
using Thousand.Model;
using Thousand.Parse;

namespace Thousand.API
{
    static class AttributeType
    {
        public static AttributeType<(T first, T second)> Twice<T>(this AttributeType<T> self) => new(
            self.Parser.Twice(),
            "one or two of: " + self.Documentation + ", for both axes or each axis",
            self.Examples
        );

        public static AttributeType<AlignmentKind> AlignColumn { get; } = new(Value.AlignColumn, "`start`/`left`, `center`/`stretch` or `end`/`right`");
        public static AttributeType<AlignmentKind?> AlignColumnOptional { get; } = new(
            Value.AlignColumn.OrNone(),
            "`start`/`left`, `center`/`stretch`, `end`/`right` or `none`"
        );
        public static AttributeType<AlignmentKind> AlignRow { get; } = new(Value.AlignRow, "`start`/`top`, `center`/`stretch` or `end`/`bottom`");
        public static AttributeType<AlignmentKind?> AlignRowOptional { get; } = new(
            Value.AlignRow.OrNone(),
            "`start`/`top`, `center`/`stretch`, `end`/`bottom` or `none`"
        );

        public static AttributeType<Anchor> Anchor { get; } = new(
            Parse.Identifier.Enum<CompassKind>().Select(k => new SpecificAnchor(k) as Anchor)
                .Or(Token.EqualToValueIgnoreCase(TokenKind.Identifier, "any").Value(AnyAnchor.Instance))
                .Or(Token.EqualToValueIgnoreCase(TokenKind.Identifier, "corners").Or(Token.EqualToValueIgnoreCase(TokenKind.Identifier, "corner")).Value(CornerAnchor.Instance))
                .OrDefault(NoAnchor.Instance),
            $"`none`, `any`, `corners` or a compass direction ({Format.Names<CompassKind>()})"
        );

        public static AttributeType<Border> Border { get; } = new(
            Value.Border, 
            "one of: `W` (number), / `X Y` (horizontal and vertical widths) /`Left Top Right Bottom` (individual widths))",
            "20", "50 0", "1 1 1 0"
        );

        public static AttributeType<Colour> Colour { get; } = new(Value.Colour, "`#rrggbb` or `#rgb` (colour)", "#000", "black");
        public static AttributeType<Colour?> ColourOptional { get; } = new(
            Value.Colour.OrNull(), 
            "`#rrggbb` or `#rgb` (colour), or `none`", 
            "#000", "black", "none"
        );

        public static AttributeType<T> Enum<T>() where T : struct, System.Enum => new(Parse.Identifier.Enum<T>(), Format.Names<T>());
        public static AttributeType<T?> EnumOptional<T>() where T : struct, System.Enum => new(Parse.Identifier.Enum<T>().OrNone(), Format.NamesOrNone<T>());

        public static AttributeType<TrackSize> GridSize { get; } = new(
            Token.EqualToValueIgnoreCase(TokenKind.Identifier, "pack").Value(PackedSize.Instance)
                .Or(Token.EqualToValueIgnoreCase(TokenKind.Identifier, "equal").Value(EqualSize.Instance))
                .Or(Value.WholeDecimal.Select(x => new MinimumSize(x) as TrackSize)),
            $"`pack`, `equal` or `X` (number)"
        );

        public static AttributeType<int> GridTrack(string name) => new(
            Value.CountingNumber.Named("grid track"), 
            $"`{name}` (row/column number), starting from 1", 
            "1", "2", "5"
        );

        public static AttributeType<decimal> PixelSize(string name) => new(
            Value.WholeDecimal,
            $"`{name}` (number)",
            "0", "0.5", "50"
        );

        public static AttributeType<Point> PointAbsolute { get; } = new(
            from x in Value.WholeNumber from y in Value.WholeNumber select new Point(x, y),
            "`X Y` (point, non-negative)", 
            "0 0", "1 0", "0 0.1"
        );
        public static AttributeType<Point> PointRelative { get; } = new(
            Value.Point, 
            "`X Y` (point)", 
            "0 0", "1 0", "-1 0.1"
        );

        public static AttributeType<string> String { get; } = new(
            Value.String, 
            "`\"Name\"` or `Name` (string)", 
            "\"My Object\""
        );

        public static AttributeType<Width> Width { get; } = new(
            Value.Width,
            "`X` (number), `hairline` (non-scaling) or `none`"
        );
    }

    record AttributeType<T>(TokenListParser<TokenKind, T> Parser, string Documentation, params string[] Examples);
}
