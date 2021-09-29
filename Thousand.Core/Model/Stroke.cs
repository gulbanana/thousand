namespace Thousand.Model
{
    public record Stroke(Colour Colour, int? Width, StrokeKind Style)
    {
        public Stroke() : this(Colour.Black, 1, StrokeKind.Solid) { }
    }
}
