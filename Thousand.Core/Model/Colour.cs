namespace Thousand.Model
{
    public record Colour(byte R, byte G, byte B)
    {
        public SkiaSharp.SKColor SK() => new SkiaSharp.SKColor(R, G, B);
    }
}
