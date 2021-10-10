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
                case ShapeKind.Roundrect:
                case ShapeKind.Roundsquare:
                    path.AddRoundRect(new SKRoundRect(bounds, shape.CornerRadius));
                    break;

                case ShapeKind.Pill:
                    path.AddRoundRect(new SKRoundRect(bounds, bounds.Height/2));
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
                    path.AddPoly(Shapes.Diamond(bounds.KS()).SK());
                    break;

                case ShapeKind.Triangle:
                    path.AddPoly(Shapes.Triangle(bounds.KS()).SK());
                    break;

                case ShapeKind.Trapezium:
                    path.AddPoly(Shapes.Trapezium(bounds.KS(), shape.CornerRadius).SK());
                    break;

                case ShapeKind.Octagon:
                    path.AddPoly(Shapes.Octagon(bounds.KS()).SK());
                    break;
            };
            path.Close();

            return path;
        }
    }
}
