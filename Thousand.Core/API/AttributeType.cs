using Superpower;
using Thousand.Model;
using Thousand.Parse;

namespace Thousand.API
{
    static class AttributeType
    {
        public static AttributeType<Point> AbsolutePoint { get; } = new AttributeType<Point>(from x in Value.WholeNumber from y in Value.WholeNumber select new Point(x, y), "`X Y` (point, non-negative)", "0 0", "5 0");
        public static AttributeType<Point> RelativePoint { get; } = new AttributeType<Point>(Value.Point, "`X Y` (point)", "0 0", "5 0", "10 -10");
        public static AttributeType<int> Column { get; } = new AttributeType<int>(Value.CountingNumber, "`X` (integer, positive)", "3");
        public static AttributeType<int> Row { get; } = new AttributeType<int>(Value.CountingNumber, "`Y` (integer, positive)", "3");
    }

    record AttributeType<T>(TokenListParser<TokenKind, T> Parser, string Documentation, params string[] Examples);
}
