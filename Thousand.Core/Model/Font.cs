namespace Thousand.Model
{
    public record Font(string Family, int Size)
    {
        public Font() : this(SkiaSharp.SKTypeface.Default.FamilyName, 20) { }
    }
}
