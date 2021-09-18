using Thousand.Model;

namespace Thousand.Render
{
    internal static class ModelExtensions
    {
        public static SkiaSharp.SKColor SK(this Colour self) => new SkiaSharp.SKColor(self.R, self.G, self.B);
    }
}
