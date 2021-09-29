using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using Thousand.Model;

namespace Thousand.Render
{
    public class Renderer : IRenderer<SKImage>, IDisposable
    {
        public Renderer()
        {
            // no shared resources yet, but there probably will be
        }

        public void Dispose()
        {
            // no shared resources yet, but there probably will be
        }

        public IReadOnlyDictionary<string, Point> MeasureTextBlocks(IR.Rules ir)
        {
            var result = new Dictionary<string, Point>();

            foreach (var t in ir.Objects.Select(o => o.Text).WhereNotNull())
            {                
                var text = new Topten.RichTextKit.RichString()
                    .FontFamily(SKTypeface.Default.FamilyName)
                    .FontSize(t.FontSize)
                    .Alignment(Topten.RichTextKit.TextAlignment.Center)
                    .Add(t.Label);

                result[t.Label] = new((int)MathF.Ceiling(text.MeasuredWidth), (int)MathF.Ceiling(text.MeasuredHeight));
            }

            return result;
        }

        public SKImage Render(Layout.Diagram diagram)
        {
            using var state = new RenderState();

            foreach (var shape in diagram.Shapes)
            {
                state.ShapePaths[shape] = state.MeasureShape(shape);
            }

            var pixelWidth = (int)(diagram.Width * diagram.Scale);
            var pixelHeight = (int)(diagram.Height * diagram.Scale);
            var info = new SKImageInfo(Math.Max(pixelWidth, 1), Math.Max(pixelHeight, 1));

            using var surface = SKSurface.Create(info);
            var canvas = surface.Canvas;

            state.PaintDiagram(canvas, diagram);

            foreach (var shape in diagram.Shapes)
            {
                state.PaintShape(canvas, shape);
            }

            foreach (var label in diagram.Labels)
            {
                state.PaintLabel(canvas, label);
            }

            foreach (var line in diagram.Lines)
            {
                state.PaintLine(canvas, line);
            }

            return surface.Snapshot();
        }
    }
}
