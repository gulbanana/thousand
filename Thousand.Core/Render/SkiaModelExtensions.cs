using SkiaSharp;
using System.Linq;
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

        public static SKPoint SK(this Point self) => new((float)self.X, (float)self.Y);
        public static SKPoint[] SK(this Point[] self) => self.Select(SK).ToArray();
        public static Point KS(this SKPoint self) => new((decimal)self.X, (decimal)self.Y);

        public static SKRect SK(this Rect self) => new((float)self.Left, (float)self.Top, (float)self.Right, (float)self.Bottom);
        public static Rect KS(this SKRect self) => new((decimal)self.Left, (decimal)self.Top, (decimal)self.Right, (decimal)self.Bottom);

        public static SKPoint Normalize(this SKPoint vector, float length = 1.0f)
        {
            return new SKPoint(vector.X / vector.Length * length, vector.Y / vector.Length * length);
        }
    }
}
