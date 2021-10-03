using Thousand.Layout;
using Xunit;
using Thousand.Model;
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
            var textBounds = new Rect(new Point(30, 28));

            var diagram = new Diagram(100, 100, 1, null, new Shape[0], new LabelBlock[]
            {
                new LabelBlock(new Font(), textBounds, "foo", new[] { new LabelLine(textBounds, "foo") })
            },
            new Line[]
            {
                new Line(new Stroke(), new Point(0, 0), new Point(100, 100), false, false)
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
        }
    }
}
