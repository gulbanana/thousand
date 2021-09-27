using Thousand.Model;

namespace Thousand.Render
{
    internal static class ModelExtensions
    {
        public static SkiaSharp.SKColor SK(this Colour self) => new(self.R, self.G, self.B);
        public static SkiaSharp.SKPoint SK(this Point self) => new(self.X, self.Y);
        public static SkiaSharp.SKRect SK(this Rect self) => new(self.Left, self.Top, self.Right, self.Bottom);
    }
}
