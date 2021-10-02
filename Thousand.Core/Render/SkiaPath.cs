using SkiaSharp;
using Thousand.Model;

namespace Thousand.Render
{
    internal static class SkiaPath
    {
        public static SKPath Create(Layout.Shape shape)
        {
            var bounds = shape.Bounds.SK();

            var path = new SKPath();
            switch (shape.Kind)
            {
                case ShapeKind.RoundRectangle:
                case ShapeKind.RoundSquare:
                    path.AddRoundRect(new SKRoundRect(bounds, shape.CornerRadius));
                    break;

                case ShapeKind.Rectangle:
                case ShapeKind.Square:
                    path.AddRect(bounds);
                    break;

                case ShapeKind.Ellipse:
                    path.AddOval(bounds);
                    break;

                case ShapeKind.Circle:
                    path.AddCircle(bounds.MidX, bounds.MidY, bounds.Width / 2);
                    break;

                case ShapeKind.Rhombus:
                case ShapeKind.Diamond:
                    path.AddPoly(Diamond(bounds));
                    break;
            };
            path.Close();

            return path;
        }

        private static SKPoint[] Diamond(SKRect box)
        {
            return new SKPoint[]
            {
                new(box.Left, box.MidY),
                new(box.MidX, box.Top),
                new(box.Right, box.MidY),
                new(box.MidX, box.Bottom),
            };
        }
    }
}
