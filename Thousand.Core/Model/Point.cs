namespace Thousand.Model
{
    public record Point(int X, int Y)
    {
        public static Point Zero { get; } = new(0, 0);

        public static Point operator +(Point a, Point b) => new(a.X + b.X, a.Y + b.Y);
        public static Point operator /(Point a, int b) => new(a.X / b, a.Y / b);

        public Point Pad(int padding) => new(X + padding * 2, Y + padding * 2);
    }
}
