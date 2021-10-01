﻿using System;
using System.Linq;
using System.Xml.Linq;
using Thousand.Layout;

namespace Thousand.Render
{
    public sealed class SVGRenderer : IRenderer<XElement>, IDisposable
    {
        private readonly XNamespace xmlns;

        public SVGRenderer()
        {
            xmlns = "http://www.w3.org/2000/svg";
        }

        public void Dispose() { }

        public XElement Render(Diagram diagram)
        {
            var state = new SVGRendererState(diagram.Scale);

            var w = diagram.Width * diagram.Scale;
            var h = diagram.Height * diagram.Scale;

            var svg = new XElement(xmlns + "svg", new XAttribute("width", w), new XAttribute("height", h), new XAttribute("viewBox", $"0 0 {diagram.Width} {diagram.Height}"),
                new XElement(xmlns + "defs",
                    diagram.Lines.Select(l => l.Stroke.Colour).Distinct().Select(state.DefineMarker).ToArray()
                )
            );

            if (diagram.Background != null)
            {
                svg.Add(new XElement(xmlns + "rect", new XAttribute("width", "100%"), new XAttribute("height", "100%"), new XAttribute("fill", diagram.Background.SVG())));
            }
            
            foreach (var tag in diagram.Shapes.Select(state.RenderShape))
            {
                svg.Add(tag);
            }

            foreach (var tag in diagram.Labels.Select(state.RenderLabel))
            {
                svg.Add(tag);
            }

            foreach (var tag in diagram.Lines.Select(state.RenderLine))
            {
                svg.Add(tag);
            }

            return svg;
        }
    }
}