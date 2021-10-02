using System;
using System.Xml.Linq;
using Thousand.Layout;
using Thousand.Model;

namespace Thousand.Render
{
    internal sealed class SVGRendererState
    {
        private readonly XNamespace xmlns;
        private readonly float scale;

        public SVGRendererState(float diagramScale)
        {
            xmlns = "http://www.w3.org/2000/svg";
            scale = diagramScale;
        }

        public XElement DefineMarker(Colour c) => new XElement(xmlns + "marker",
            new XAttribute("id", DeclareMarker(c)),
            new XAttribute("fill", c.SVG()),
            new XAttribute("orient", "auto"),
            new XAttribute("markerUnits", "userSpaceOnUse"),
            new XAttribute("markerWidth", 7), new XAttribute("markerHeight", 8),
            new XAttribute("refX", 7), new XAttribute("refY", 4),
            new XElement(xmlns + "polygon", new XAttribute("points", "0 8 7 4 0 0"))
        );

        public XElement RenderShape(Shape shape)
        {
            var tag = CreatePath(shape.Kind.Value, shape.Bounds, shape.CornerRadius);

            tag.Add(new XAttribute("fill", shape.Fill.SVG()));
            tag.Add(shape.Stroke.SVG(scale));

            return tag;
        }

        public XElement RenderLine(Line line)
        {
            var tag = new XElement(xmlns + "line",
                new XAttribute("x1", line.Start.X),
                new XAttribute("y1", line.Start.Y),
                new XAttribute("x2", line.End.X),
                new XAttribute("y2", line.End.Y),
                new XAttribute("marker-end", $"url(#{DeclareMarker(line.Stroke.Colour)})")
            );

            tag.Add(line.Stroke.SVG(scale));

            return tag;
        }

        public XElement RenderLabel(Label label)
        {
            return new XElement(xmlns + "text",
                new XAttribute("dominant-baseline", "text-before-edge"),
                new XAttribute("x", label.Bounds.Left),
                new XAttribute("y", label.Bounds.Top),
                new XAttribute("font-family", "Segoe UI"),
                new XAttribute("font-size", label.FontSize + "px"),
                new XAttribute("fill", "rgb(0,0,0)"),
                label.Content
            );
        }

        private string DeclareMarker(Colour c)
        {
            return "arrow-" + Convert.ToHexString(new[] { c.R, c.G, c.B });
        }

        private XElement CreatePath(ShapeKind kind, Rect bounds, int corner)
        {
            var cx = bounds.SK().MidX;
            var cy = bounds.SK().MidY;

            return kind switch
            {
                ShapeKind.Rhombus or ShapeKind.Diamond => new XElement(xmlns + "path", 
                    new XAttribute("d", $"M {cx} {bounds.Top} {bounds.Right} {cy} {cx} {bounds.Bottom} {bounds.Left} {cy} Z")
                ),

                ShapeKind.Ellipse or ShapeKind.Circle => new XElement(xmlns + "ellipse", 
                    new XAttribute("cx", cx), 
                    new XAttribute("cy", cy), 
                    new XAttribute("rx", bounds.Width / 2f), 
                    new XAttribute("ry", bounds.Height / 2f)
                ),

                ShapeKind.RoundRectangle or ShapeKind.RoundSquare => new XElement(xmlns + "rect",
                    new XAttribute("x", bounds.Left),
                    new XAttribute("y", bounds.Top),
                    new XAttribute("width", bounds.Width),
                    new XAttribute("height", bounds.Height),
                    new XAttribute("rx", corner)
                ),

                _ => new XElement(xmlns + "rect",
                    new XAttribute("x", bounds.Left),
                    new XAttribute("y", bounds.Top),
                    new XAttribute("width", bounds.Width),
                    new XAttribute("height", bounds.Height)
                )
            };
        }
    }
}
