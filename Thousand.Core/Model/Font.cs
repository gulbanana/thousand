namespace Thousand.Model
{
    public record Font(string Family, int Size, Colour Colour)
    {
        public Font() : this(SkiaSharp.SKTypeface.Default.FamilyName, 20, Colour.Black) { }
    }
}
