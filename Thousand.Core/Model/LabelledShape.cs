namespace Thousand.Model
{
    public record LabelledShape(int Row, int Column, string Label, ShapeKind Kind, Colour Stroke, Colour Fill);
}
