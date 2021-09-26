using Thousand.Model;

// Intermediate representation shared between Canonicalise and Compose stages
namespace Thousand.IR
{        
    internal record Config(float Scale, Colour Background);
    internal record Object(string? Name, int Row, int Column, string? Label, ShapeKind Kind, Colour Stroke, Colour Fill, float FontSize, float? StrokeWidth);
    internal record Edge(Object FromTarget, Object ToTarget, Point FromOffset, Point ToOffset, Colour Stroke, float? Width);
}
