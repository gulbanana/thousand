namespace Thousand.Model
{
    public record Stroke(Colour Colour, Width Width, StrokeKind Style)
    {
        public Stroke() : this(Colour.Black, new HairlineWidth(), StrokeKind.Solid) { }
        public Stroke(Width width) : this(Colour.Black, width, StrokeKind.Solid) { }
    }
}
