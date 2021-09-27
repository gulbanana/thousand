using System.Collections.Generic;
using Thousand.Model;

// Intermediate representation shared between Canonicalise and Compose stages
namespace Thousand.IR
{        
    public record Config(float Scale, Colour Background);
    public record Object(string? Name, int? Row, int? Column, int? Width, int? Height, string? Label, ShapeKind Kind, int Padding, Colour Stroke, Colour Fill, float FontSize, float? StrokeWidth);
    public record Edge(Object FromTarget, Object ToTarget, Point FromOffset, Point ToOffset, Colour Stroke, float? Width);
    public record Rules(Config Config, IReadOnlyList<Object> Objects, IReadOnlyList<Edge> Edges);
}
