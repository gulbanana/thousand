using System.Collections.Generic;
using Thousand.Model;

namespace Thousand.Layout
{
    public record Label(int X, int Y, string Content);
    public record Shape(int X, int Y, ShapeKind Kind, Label Fit, Colour Stroke, Colour Fill);
    public record Line(int X1, int Y1, int X2, int Y2, Colour Stroke);
    public record Diagram(int Width, int Height, IReadOnlyList<Shape> Shapes, IReadOnlyList<Label> Labels, IReadOnlyList<Line> Lines);
}
