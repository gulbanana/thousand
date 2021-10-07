namespace Thousand.Model
{
    public sealed record Connector(Point Location, bool IsCorner)
    {
        public Connector(decimal x, decimal y, bool isCorner) : this(new Point(x, y), isCorner) { }
    }
}
