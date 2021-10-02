using SkiaSharp;
using Thousand.Model;

namespace Thousand.Render
{
    internal static class SkiaModelExtensions
    {
        public static SKColor SK(this Colour? self)
        {
            if (self == null)
            {
                return SKColors.Transparent;
            }
            else
            {
                return new(self.R, self.G, self.B);
            }            
        }

        public static SKPoint SK(this Point self) => new(self.X, self.Y);

        public static SKRect SK(this Rect self) => new(self.Left, self.Top, self.Right, self.Bottom);

        public static SKPoint Normalize(this SKPoint vector, float length = 1.0f)
        {
            return new SKPoint(vector.X / vector.Length * length, vector.Y / vector.Length * length);
        }
    }
}
