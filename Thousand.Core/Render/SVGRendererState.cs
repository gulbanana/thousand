using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Thousand.Layout;
using Thousand.Model;

namespace Thousand.Render
{
    internal sealed class SVGRendererState
    {
        private readonly XNamespace xmlns;
        private readonly Stack<decimal> scale;

        public SVGRendererState()
        {
            xmlns = "http://www.w3.org/2000/svg";
            scale = new Stack<decimal>();
            scale.Push(1m);
        }

        public XElement DefineMarker(Colour c) => new XElement(xmlns + "marker",
            new XAttribute("id", DeclareMarker(c)),
            c.SVG("fill"),
            new XAttribute("orient", "auto-start-reverse"),
            new XAttribute("markerUnits", "userSpaceOnUse"),
            new XAttribute("markerWidth", 7), new XAttribute("markerHeight", 8),
            new XAttribute("refX", 7), new XAttribute("refY", 4),
            new XElement(xmlns + "polygon", new XAttribute("points", "0 8 7 4 0 0"))
        );

        public IEnumerable<XElement> ProcessCommandList(IReadOnlyList<Command> commands)
        {
            foreach (var command in commands)
            {
                switch (command)
                {
                    case Drawing drawing:
                        yield return RenderShape(drawing);
                        break;

                    case Line line:
                        yield return RenderLine(line);
                        break;

                    case Label label:
                        yield return RenderLabel(label);
                        break;

                    case Transform transform:                        
                        var group = new XElement(xmlns + "g", new XAttribute("transform", $"scale({transform.Scale} {transform.Scale})"));
                        scale.Push(transform.Scale);
                        foreach (var tag in ProcessCommandList(transform.Commands))
                        {
                            group.Add(tag);
                        }
                        scale.Pop();
                        yield return group;
                        break;
                }
            }
        }

        public XElement RenderShape(Drawing drawing)
        {
            var tag = CreatePath(drawing);

            tag.Add(drawing.Fill.SVG("fill"));
            tag.Add(drawing.Stroke.SVG(scale.Peek()));

            return tag;
        }

        public XElement RenderLine(Line line)
        {
            var tag = new XElement(xmlns + "line",
                new XAttribute("x1", line.Start.X),
                new XAttribute("y1", line.Start.Y),
                new XAttribute("x2", line.End.X),
                new XAttribute("y2", line.End.Y)
            );

            if (line.StartMarker)
            {
                tag.Add(new XAttribute("marker-start", $"url(#{DeclareMarker(line.Stroke.Colour)})"));
            }

            if (line.EndMarker)
            {
                tag.Add(new XAttribute("marker-end", $"url(#{DeclareMarker(line.Stroke.Colour)})"));
            }

            tag.Add(line.Stroke.SVG(scale.Peek()));

            return tag;
        }

        private XElement RenderLabel(Label label)
        {
            var tag = new XElement(xmlns + "text",
                new XAttribute("dominant-baseline", "text-before-edge"),
                new XAttribute("x", label.Bounds.Left),
                new XAttribute("y", label.Bounds.Top),
                new XAttribute("font-family", label.Font.Family),
                new XAttribute("font-size", label.Font.Size + "px"),
                label.Font.Colour.SVG("fill")
            );

            foreach (var line in label.Lines)
            {
                tag.Add(new XElement(xmlns + "tspan",
                    new XAttribute("x", line.Bounds.Left),
                    new XAttribute("y", line.Bounds.Top),
                    line.Content
                ));
            }

            return tag;
        }

        private string DeclareMarker(Colour c)
        {
            return $"arrow-{Convert.ToHexString(new[] { c.R, c.G, c.B })}";
        }

        private XElement CreatePath(Drawing drawing)
        {
            var cx = drawing.Bounds.SK().MidX;
            var cy = drawing.Bounds.SK().MidY;

            return drawing.Shape.Style switch
            {
                ShapeKind.Rhombus or ShapeKind.Diamond => new XElement(xmlns + "path", 
                    new XAttribute("d", Shapes.Diamond(drawing.Bounds).SVG())
                ),

                ShapeKind.Triangle => new XElement(xmlns + "path",
                    new XAttribute("d", Shapes.Triangle(drawing.Bounds).SVG())
                ),

                ShapeKind.Trapezium => new XElement(xmlns + "path",
                    new XAttribute("d", Shapes.Trapezium(drawing.Bounds, drawing.Shape.CornerRadius).SVG())
                ),

                ShapeKind.Octagon => new XElement(xmlns + "path",
                    new XAttribute("d", Shapes.Octagon(drawing.Bounds).SVG())
                ),

                ShapeKind.Ellipse or ShapeKind.Circle => new XElement(xmlns + "ellipse", 
                    new XAttribute("cx", cx), 
                    new XAttribute("cy", cy), 
                    new XAttribute("rx", drawing.Bounds.Width / 2m), 
                    new XAttribute("ry", drawing.Bounds.Height / 2m)
                ),

                ShapeKind.Roundrect or ShapeKind.Roundsquare => new XElement(xmlns + "rect",
                    new XAttribute("x", drawing.Bounds.Left),
                    new XAttribute("y", drawing.Bounds.Top),
                    new XAttribute("width", drawing.Bounds.Width),
                    new XAttribute("height", drawing.Bounds.Height),
                    new XAttribute("rx", drawing.Shape.CornerRadius)
                ),

                ShapeKind.Pill => new XElement(xmlns + "rect",
                    new XAttribute("x", drawing.Bounds.Left),
                    new XAttribute("y", drawing.Bounds.Top),
                    new XAttribute("width", drawing.Bounds.Width),
                    new XAttribute("height", drawing.Bounds.Height),
                    new XAttribute("rx", drawing.Bounds.Height/2)
                ),

                _ => new XElement(xmlns + "rect",
                    new XAttribute("x", drawing.Bounds.Left),
                    new XAttribute("y", drawing.Bounds.Top),
                    new XAttribute("width", drawing.Bounds.Width),
                    new XAttribute("height", drawing.Bounds.Height)
                )
            };
        }
    }
}
