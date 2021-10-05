using Thousand.Model;

namespace Thousand.Render
{
    internal static class Shapes
    {
        public static Point[] Diamond(Rect box) => new Point[]
        {
            new(box.Left, box.Center.Y),
            new(box.Center.X, box.Top),
            new(box.Right, box.Center.Y),
            new(box.Center.X, box.Bottom),
        };

        public static Point[] Triangle(Rect box) => new Point[]
        {
            new(box.Left, box.Bottom),
            new(box.Center.X, box.Top),
            new(box.Right, box.Bottom)
        };

        public static Point[] Trapezium(Rect box, int corner) => new Point[]
        {
            new(box.Left, box.Bottom),
            new(box.Left+corner, box.Top),
            new(box.Right-corner, box.Top),
            new(box.Right, box.Bottom)
        };
    }
}
