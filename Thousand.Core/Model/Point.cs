namespace Thousand.Model
{
    public record Point(int X, int Y)
    {
        public static Point Origin { get; } = new(0, 0);

        public static Point operator +(Point a, Point b) => new(a.X + b.X, a.Y + b.Y);
    }
}
