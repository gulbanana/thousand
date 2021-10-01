﻿using Thousand.Layout;
using Xunit;
using Thousand.Model;
using Thousand.Compose;
using System.Xml.Linq;

namespace Thousand.Tests
{
    public class SVG
    {
        private readonly XNamespace xmlns;
        private readonly Render.SVGRenderer renderer;

        public SVG()
        {
            xmlns = "http://www.w3.org/2000/svg";
            renderer = new Render.SVGRenderer();
        }

        [Fact]
        public void IntegrationTest()
        {
            var diagram = new Diagram(100, 100, 1f, null, new Shape[0], new Label[]
            {
                new Label(new Rect(Measure.TextBlock(new IR.Text("foo", 20))), "foo", 20)
            },
            new Line[]
            {
                new Line(null, null, new Point(0, 0), new Point(100, 100), new Stroke())
            });

            var svg = renderer.Render(diagram);

            Assert.Single(svg.Elements(xmlns + "text"));
            Assert.Equal("foo", svg.Element(xmlns + "text")!.Value);
            Assert.Equal("20px", svg.Element(xmlns + "text")!.Attribute("font-size")!.Value);

            Assert.Single(svg.Elements(xmlns + "line"));
            Assert.Equal("0", svg.Element(xmlns + "line")!.Attribute("x1")!.Value);
            Assert.Equal("100", svg.Element(xmlns + "line")!.Attribute("x2")!.Value);
            Assert.Equal("0", svg.Element(xmlns + "line")!.Attribute("y1")!.Value);
            Assert.Equal("100", svg.Element(xmlns + "line")!.Attribute("y2")!.Value);
        }
    }
}