namespace Thousand.Model
{
    public record Rect(decimal Left, decimal Top, decimal Right, decimal Bottom)
    {
        public decimal Width => Right - Left;
        public decimal Height => Bottom - Top;
        public Point Origin => new Point(Left, Top);
        public Point Center => new Point(Left + (Right - Left) / 2, Top + (Bottom - Top) / 2);

        public Rect(Point size) : this(0, 0, size.X, size.Y) { }
        public Rect(Point origin, Point size) : this(origin.X, origin.Y, origin.X + size.X, origin.Y + size.Y) { }

        public Rect CenteredAt(Point center) => new Rect(
            new Point(center.X - (Width / 2), center.Y - (Height / 2)),
            new Point(Width, Height)
        );

        public Rect PadFromOrigin(int padding)
        {
            return new Rect(Left - padding, Top - padding, Right + padding, Bottom + padding);
        }

        public Rect PadFromCenter(int padding)
        {
            return new Rect(Left - padding, Top - padding, Right + padding, Bottom + padding);
        }

        public Rect Square()
        {
            if (Width > Height)
            {
                var radius = Width / 2;
                return new Rect(Left, Center.Y - radius, Right, Center.Y + radius);
            }
            else
            {
                var radius = Height / 2;
                return new Rect(Center.X - radius,Top, Center.X + radius, Bottom);
            }
        }

        public Rect Grow(int toWidth, int toHeight)
        {
            return new Rect(0, 0, toWidth, toHeight).CenteredAt(Center);
        }
    }
}
