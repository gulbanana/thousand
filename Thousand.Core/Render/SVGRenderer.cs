using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Thousand.Render
{
    public sealed partial class SVGRenderer : IRenderer<XElement>, IDisposable
    {
        private readonly XNamespace xmlns;

        public SVGRenderer()
        {
            xmlns = "http://www.w3.org/2000/svg";
        }

        public void Dispose() { }

        public XElement Render(Layout.Diagram diagram)
        {
            var state = new SVGRendererState();

            var w = diagram.Width;
            var h = diagram.Height;            

            var svg = new XElement(xmlns + "svg", new XAttribute("width", w), new XAttribute("height", h), new XAttribute("viewBox", $"0 0 {diagram.Width} {diagram.Height}"),
                // MDN claims that WebKit supports geometricPrecision, which disables hinting, but that in Gecko it is ignored and treated as optimizeLegibility.
                // However, I observe the opposite: in Firefox, geometricPrecision enables sub-pixel text positioning, and in Chrome it seems to do nothing!
                // I prefer the Chrome geometricPrecision behaviour, which is consistent with Skia, but I prefer cross-browser compatibility more.
                new XAttribute("text-rendering", "optimizeLegibility"),

                new XElement(xmlns + "defs",
                    WalkLines(diagram.Commands).Select(l => l.Stroke.Colour).Distinct().Select(state.DefineMarker).ToArray() // XXX
                )
            );

            foreach (var tag in state.ProcessCommandList(diagram.Commands))
            {
                svg.Add(tag);
            }

            return svg;
        }

        private static IEnumerable<Layout.Line> WalkLines(IReadOnlyList<Layout.Command> commands)
        {
            foreach (var command in commands)
            {
                switch (command)
                {
                    case Layout.Line line:
                        yield return line;
                        break;

                    case Layout.Transform transform:
                        foreach (var line in WalkLines(transform.Commands))
                        {
                            yield return line;
                        }
                        break;
                }
            }
        }
    }
}
