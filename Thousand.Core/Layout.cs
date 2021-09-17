using System.Collections.Generic;

namespace Thousand.Layout
{
    public record Label(int X, int Y, string Content);
    public record Shape(int X, int Y, ShapeKind Kind, Label Fit);
    public record Line(int X1, int Y1, int X2, int Y2);
    public record Diagram(int Width, int Height, IReadOnlyList<Shape> Shapes, IReadOnlyList<Label> Labels, IReadOnlyList<Line> Lines);
}
