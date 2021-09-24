using Thousand.Model;

namespace Thousand.IR
{        
    internal record Object(string? Name, int Row, int Column, string? Label, ShapeKind Kind, Colour Stroke, Colour Fill, float FontSize);
    internal record Edge(Object FromTarget, Object ToTarget, Colour Stroke);
}
