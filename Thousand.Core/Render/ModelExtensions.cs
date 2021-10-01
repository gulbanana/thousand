using Thousand.Model;

namespace Thousand.Render
{
    internal static class ModelExtensions
    {
        public static SkiaSharp.SKColor SK(this Colour? self)
        {
            if (self == null)
            {
                return SkiaSharp.SKColors.Transparent;
            }
            else
            {
                return new(self.R, self.G, self.B);
            }            
        }

        public static string SVG(this Colour? self)
        {
            if (self == null)
            {
                return "transparent";
            }
            else
            {
                return $"rgb({self.R},{self.G},{self.B})";
            }
        }

        public static string SVG(this Width self)
        {
            return self switch
            {
                HairlineWidth => @"stroke-width=""1"" vector-effect=""non-scaling-stroke""",
                PositiveWidth(var x) => $@"stroke-width=""{x}""",
                ZeroWidth or _ => @"stroke-width=""0""",
            };
        }

        public static string SVG(this Stroke self, float scale)
        {
            var width = self.Width switch
            {
                HairlineWidth => @"stroke-width=""1"" vector-effect=""non-scaling-stroke""",
                PositiveWidth(var x) => $@"stroke-width=""{x}""",
                ZeroWidth or _ => @"stroke-width=""0""",
            };

            var dashScale = self.Width is HairlineWidth ? scale : 1f;

            return $@"stroke=""{self.Colour.SVG()}"" {self.Width.SVG()}" + self.Style switch
            {
                StrokeKind.Dashed => $@" stroke-dasharray=""{3 * dashScale} {2 * dashScale}""",
                StrokeKind.Solid or _ => string.Empty                
            };
        }

        public static SkiaSharp.SKPoint SK(this Point self) => new(self.X, self.Y);
        public static SkiaSharp.SKRect SK(this Rect self) => new(self.Left, self.Top, self.Right, self.Bottom);
    }
}
