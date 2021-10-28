using System.Collections.Generic;
using Thousand.Model;

// Intermediate representation shared between Compose and Render stages
namespace Thousand.Layout
{
    /// <summary>Drawing commands, back-to-front.</summary>
    public abstract record Command;

    public record LabelSpan(Rect Bounds, string Content);
    public record Label(Font Font, Rect Bounds, string Content, IReadOnlyList<LabelSpan> Lines) : Command;

    public record Shape(Rect Bounds, ShapeKind Kind, int CornerRadius, Stroke Stroke, Colour? Fill) : Command;

    public record Line(Stroke Stroke, Point Start, Point End, bool StartMarker, bool EndMarker) : Command;

    public record Transform(decimal Scale, IReadOnlyList<Command> Commands) : Command;

    public record Diagram(int Width, int Height, IReadOnlyList<Command> Commands);
}
