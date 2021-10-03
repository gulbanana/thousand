using System.Collections.Generic;
using Thousand.Model;

// Intermediate representation shared between Compose and Render stages
namespace Thousand.Layout
{
    public record LabelLine(Rect Bounds, string Content);
    public record LabelBlock(Font Font, Rect Bounds, string Content, IReadOnlyList<LabelLine> Lines);
    public record Shape(Rect Bounds, ShapeKind Kind, int CornerRadius, Stroke Stroke, Colour Fill); // XXX should not have Name
    public record Line(Stroke Stroke, Point Start, Point End, bool StartMarker, bool EndMarker);
    public record Diagram(int Width, int Height, decimal Scale, Colour? Background, IReadOnlyList<Shape> Shapes, IReadOnlyList<LabelBlock> Labels, IReadOnlyList<Line> Lines);
}
