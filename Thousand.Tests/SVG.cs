using Thousand.Layout;
using Xunit;
using Thousand.Model;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Thousand.Tests
{
    public class SVG
    {
        private readonly XNamespace xmlns;
        private readonly Render.SVGRenderer renderer;

        public SVG()
        {
            xmlns = "http://www.w3.org/2000/svg";
            renderer = new Render.SVGRenderer(false);
        }

        [Fact]
        public void IntegrationTest()
        {
            var textBounds = new Rect(new Point(30, 28));

            var diagram = new Diagram(100, 100, new List<Command>
            {
                new Label(Array.Empty<string>(), new Font(), textBounds, "foo", new[] { new LabelSpan(textBounds, "foo") }),
                new Line(Array.Empty<string>(), new Stroke(), new Point(0, 0), new Point(100, 100), false, false),
                new Drawing(Array.Empty<string>(), new Shape(ShapeKind.Trapezoid, 5), new Rect(0, 0, 100, 100), new Stroke(), null),
            });

            var svg = renderer.Render(diagram);

            Assert.Empty(svg.Elements("marker"));

            Assert.Single(svg.Elements(xmlns + "text"));
            Assert.Equal("foo", svg.Element(xmlns + "text")!.Value);
            Assert.Equal("20px", svg.Element(xmlns + "text")!.Attribute("font-size")!.Value);

            Assert.Single(svg.Elements(xmlns + "line"));
            Assert.Equal("0", svg.Element(xmlns + "line")!.Attribute("x1")!.Value);
            Assert.Equal("100", svg.Element(xmlns + "line")!.Attribute("x2")!.Value);
            Assert.Equal("0", svg.Element(xmlns + "line")!.Attribute("y1")!.Value);
            Assert.Equal("100", svg.Element(xmlns + "line")!.Attribute("y2")!.Value);

            Assert.Single(svg.Elements(xmlns + "path"));
            Assert.Equal("M 0 100 5 0 95 0 100 100 Z", svg.Element(xmlns + "path")!.Attribute("d")!.Value);
        }

        [Fact]
        public void Transparency()
        {
            var diagram = new Diagram(20, 10, new List<Command>
            {
                new Drawing(Array.Empty<string>(), new Shape(ShapeKind.Rect), new Rect(0, 0, 10, 10), new Stroke(new NoWidth()), Colour.Red),
                new Drawing(Array.Empty<string>(), new Shape(ShapeKind.Rect), new Rect(10, 0, 20, 10), new Stroke(new NoWidth()), null)
            });

            var svg = renderer.Render(diagram);

            var boxes = svg.Elements(xmlns + "rect");
            Assert.Equal(2, boxes.Count());

            var left = boxes.Single(box => box.Attribute("x")!.Value == "0");
            Assert.Equal("rgb(255,0,0)", left.Attribute("fill")!.Value);

            var right = boxes.Single(box => box.Attribute("x")!.Value == "10");
            Assert.Equal("0", right.Attribute("fill-opacity")!.Value);
        }
    }
}
