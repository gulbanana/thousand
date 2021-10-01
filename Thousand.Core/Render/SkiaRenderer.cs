using SkiaSharp;
using System;

namespace Thousand.Render
{
    public sealed class SkiaRenderer : IRenderer<SKImage>, IDisposable
    {
        public SkiaRenderer()
        {
            // no shared resources yet, but there probably will be
        }

        public void Dispose()
        {
            // no shared resources yet, but there probably will be
        }

        public SKImage Render(Layout.Diagram diagram)
        {
            var state = new SkiaRenderState();

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
