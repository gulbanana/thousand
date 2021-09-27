namespace Thousand.Model
{
    public record Rect(int Left, int Top, int Right, int Bottom)
    {
        public int Width => Right - Left;
        public int Height => Bottom - Top;

        public Rect(Point size) : this(0, 0, size.X, size.Y) { }
        public Rect(Point origin, Point size) : this(origin.X, origin.Y, origin.X + size.X, origin.Y + size.Y) { }

        public Point Origin() => new Point(Left, Top);

        public Point Center() => new Point(Left + (Right - Left) / 2, Top + (Bottom - Top) / 2);

        public Rect CenteredAt(Point center) => new Rect(
            new Point(center.X - (Width / 2), center.Y - (Height / 2)),
            new Point(Width, Height)
        );

        public Rect Pad(int padding)
        {
            return new Rect(Left - padding, Top - padding, Right + padding, Bottom + padding);
        }

        public Rect Square()
        {
            var mid = Center();
            if (Width > Height)
            {
                var radius = Width / 2;
                return new Rect(Left, mid.Y - radius, Right, mid.Y + radius);
            }
            else
            {
                var radius = Height / 2;
                return new Rect(mid.X - radius,Top, mid.X + radius, Bottom);
            }
        }
    }
}
