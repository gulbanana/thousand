namespace Thousand.Model
{
    public record Object(string? Name, int Row, int Column, string? Label, ShapeKind Kind, Colour Stroke, Colour Fill);
}
