using SkiaSharp;
using System;
using System.Collections.Generic;
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

            foreach (var o in ir.Objects)
            {
                if (o.Label != null)
                {
                    var text = new Topten.RichTextKit.RichString()
                        .FontFamily(SKTypeface.Default.FamilyName)
                        .FontSize(o.FontSize)
                        .Alignment(Topten.RichTextKit.TextAlignment.Center)
                        .Add(o.Label);

                    result[o.Label] = new((int)MathF.Ceiling(text.MeasuredWidth), (int)MathF.Ceiling(text.MeasuredHeight));
                }
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
            var info = new SKImageInfo(pixelWidth, pixelHeight);

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
