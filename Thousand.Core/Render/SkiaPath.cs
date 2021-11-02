using SkiaSharp;
using Thousand.Model;

namespace Thousand.Render
{
    internal static class SkiaPath
    {
        public static SKPath Create(Layout.Drawing shape)
        {
            return Create(shape.Shape, shape.Bounds);
        }

        public static SKPath Create(Shape shape, Rect bounds)
        {
            var path = new SKPath();
            switch (shape.Style)
            {
                case ShapeKind.Roundrect:
                case ShapeKind.Roundsquare:
                    path.AddRoundRect(new SKRoundRect(bounds.SK(), (float)shape.CornerRadius));
                    break;

                case ShapeKind.Pill:
                    path.AddRoundRect(new SKRoundRect(bounds.SK(), (float)bounds.Height/2));
                    break;

                case ShapeKind.Rectangle:
                case ShapeKind.Square:
                    path.AddRect(bounds.SK());
                    break;

                case ShapeKind.Ellipse:
                    path.AddOval(bounds.SK());
                    break;

                case ShapeKind.Circle:
                    var skBounds = bounds.SK();
                    path.AddCircle(skBounds.MidX, skBounds.MidY, skBounds.Width / 2);
                    break;

                case ShapeKind.Rhombus:
                case ShapeKind.Diamond:
                    path.AddPoly(Shapes.Diamond(bounds).SK());
                    break;

                case ShapeKind.Triangle:
                    path.AddPoly(Shapes.Triangle(bounds).SK());
                    break;

                case ShapeKind.Trapezium:
                    path.AddPoly(Shapes.Trapezium(bounds, shape.CornerRadius).SK());
                    break;

                case ShapeKind.Octagon:
                    path.AddPoly(Shapes.Octagon(bounds).SK());
                    break;
            };
            path.Close();

            return path;
        }
    }
}
