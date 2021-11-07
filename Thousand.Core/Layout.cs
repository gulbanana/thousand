using System.Collections.Generic;
using Thousand.Model;

// Intermediate representation shared between Compose and Render stages
namespace Thousand.Layout
{
    /// <summary>Drawing commands, back-to-front.</summary>
    public abstract record Command(string[] Classes);

    public record LabelSpan(Rect Bounds, string Content);
    public record Label(string[] Classes, Font Font, Rect Bounds, string Content, IReadOnlyList<LabelSpan> Lines) : Command(Classes);

    public record Line(string[] Classes, Stroke Stroke, Point Start, Point End, bool StartMarker, bool EndMarker) : Command(Classes);

    public record Drawing(string[] Classes, Shape Shape, Rect Bounds, Stroke Stroke, Colour? Fill) : Command(Classes);

    public record Transform(string[] Classes, decimal? Scale, IReadOnlyList<Command> Commands) : Command(Classes);

    public record Diagram(int Width, int Height, IReadOnlyList<Command> Commands);
}
