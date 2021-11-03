using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Thousand.Model;

namespace Thousand.Render
{
    internal static class SVGModelExtensions
    {
        public static XAttribute SVG(this Colour? self, string tag)
        {
            if (self == null)
            {
                return new(tag + "-opacity", 0);
            }
            else
            {
                return new(tag, $"rgb({self.R},{self.G},{self.B})");
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

            attributes.Add(self.Colour.SVG("stroke"));

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

            var dashScale = self.Width is HairlineWidth ? scale : 1m;
            switch (self.Style)
            {
                case StrokeKind.Solid:
                    break;

                default:
                    attributes.Add(new("stroke-dasharray", string.Join(' ', Shapes.Dashes(self.Style).Select(x => (x * dashScale).ToString()))));
                    break;
            }

            return attributes.ToArray();
        }
    }
}
