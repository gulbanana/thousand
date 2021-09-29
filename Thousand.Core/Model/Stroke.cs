namespace Thousand.Model
{
    public record Stroke(Colour Colour, Width Width, StrokeKind Style)
    {
        public Stroke() : this(Colour.Black, new HairlineWidth(), StrokeKind.Solid) { }
    }
}
