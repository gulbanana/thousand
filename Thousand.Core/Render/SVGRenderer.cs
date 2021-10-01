using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Thousand.Layout;
using Thousand.Model;

namespace Thousand.Render
{
    public sealed class SVGRenderer : IRenderer<string>, IDisposable
    {
        public void Dispose() { }

        public string Render(Diagram diagram)
        {
            var svg = new StringBuilder();

            var w = diagram.Width * diagram.Scale;
            var h = diagram.Height * diagram.Scale;
            
            svg.AppendLine($@"<svg xmlns=""http://www.w3.org/2000/svg"" width=""{w}"" height=""{h}"" viewbox=""0 0 {diagram.Width} {diagram.Height}"">");
            
            svg.AppendLine(@"<defs>");
            svg.AppendLine(@"    <marker id=""arrow"" markerUnits=""userSpaceOnUse"" markerWidth=""7"" markerHeight=""8"" refX=""7"" refY=""4"" orient=""auto"">");
            svg.AppendLine(@"        <polygon points=""0 8 7 4 0 0"" />");
            svg.AppendLine(@"    </marker>");
            svg.AppendLine(@"</defs>");

            //clear
            svg.AppendLine($@"    <rect width=""100%"" height=""100%"" fill=""{diagram.Background.SVG()}"" />");

            foreach (var tag in diagram.Shapes.Select(s => RenderShape(s, diagram.Scale)))
            {
                svg.Append("    ");
                svg.AppendLine(tag);
            }

            foreach (var label in diagram.Labels)
            {
                svg.AppendLine($@"    <text x=""{label.Bounds.Left}"" y=""{label.Bounds.Top}"" font-size=""{label.FontSize}px"" font-family=""Segoe UI"" fill=""rgb(0,0,0)"" dominant-baseline=""text-before-edge"">{label.Content}</text>");
            }

            foreach (var line in diagram.Lines)
            {
                svg.AppendLine($@"    <line x1=""{line.Start.X}"" y1=""{line.Start.Y}"" x2=""{line.End.X}"" y2=""{line.End.Y}"" {line.Stroke.SVG(diagram.Scale)} marker-end=""url(#arrow)"" />");
            }

            svg.Append("</svg>");

            return svg.ToString();
        }

        private string RenderShape(Shape shape, float scale)
        {
            return $@"<{CreateShapePath(shape.Kind.Value, shape.Bounds, shape.CornerRadius)} fill=""{shape.Fill.SVG()}"" {shape.Stroke.SVG(scale)} />";
        }

        private string CreateShapePath(ShapeKind kind, Rect bounds, int corner)
        {
            var cx = bounds.SK().MidX;
            var cy = bounds.SK().MidY;

            return kind switch
            {
                ShapeKind.Diamond => $@"path d=""M {cx} {bounds.Top} {bounds.Right} {cy} {cx} {bounds.Bottom} {bounds.Left} {cy} Z""",
                ShapeKind.Oval or ShapeKind.Circle => $@"ellipse cx=""{cx}"" cy=""{cy}"" rx=""{bounds.Width / 2f}"" ry=""{bounds.Height / 2f}""",
                ShapeKind.RoundRect or ShapeKind.RoundSquare => $@"rect x=""{bounds.Left}"" y=""{bounds.Top}"" width=""{bounds.Width}"" height=""{bounds.Height}"" rx=""{corner}""",
                _ => $@"rect x=""{bounds.Left}"" y=""{bounds.Top}"" width=""{bounds.Width}"" height=""{bounds.Height}"""
            };
        }
    }
}
