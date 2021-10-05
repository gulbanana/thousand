using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using Thousand.Model;

namespace Thousand.Render
{
    internal static class SVGModelExtensions
    {
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

        public static string SVG(this Point[] self)
        {
            var builder = new StringBuilder();
            
            builder.Append("M ");
            foreach (var p in self)
            {
                builder.Append($"{p.X} {p.Y} ");
            }
            builder.Append("Z");

            return builder.ToString();
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

        public static XAttribute[] SVG(this Stroke self, decimal scale)
        {
            var attributes = new List<XAttribute>();

            attributes.Add(new("stroke", self.Colour.SVG()));

            switch (self.Width)
            {
                case ZeroWidth:
                    attributes.Add(new("stroke-width", 0));
                    break;

                case HairlineWidth:
                    attributes.Add(new("stroke-width", 1));
                    attributes.Add(new("vector-effect", "non-scaling-stroke"));
                    break;

                case PositiveWidth(var x):
                    attributes.Add(new("stroke-width", x));
                    break;
            };

            switch (self.Style)
            {
                case StrokeKind.Dashed:
                    var dashScale = self.Width is HairlineWidth ? scale : 1m;
                    attributes.Add(new("stroke-dasharray", $"{3 * dashScale} {2 * dashScale}"));
                    break;
            }

            return attributes.ToArray();
        }

        public static string SVG2(this Stroke self, float scale)
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
    }
}
