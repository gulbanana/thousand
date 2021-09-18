using System.Collections.Generic;
using Thousand.Model;

namespace Thousand.Layout
{
    public record Label(int X, int Y, string Content);
    public record Shape(int X, int Y, ShapeKind Kind, Label Fit, Colour Stroke, Colour Fill);
    public record Line(Shape From, Shape To, Colour Stroke);
    public record Diagram(int Width, int Height, IReadOnlyList<Shape> Shapes, IReadOnlyList<Label> Labels, IReadOnlyList<Line> Lines);
}
