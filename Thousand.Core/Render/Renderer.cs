using SkiaSharp;
using System;

namespace Thousand.Render
{
    public class Renderer : IDisposable
    {
        public Renderer()
        {
            // no shared resources yet, but there probably will be
        }

        public void Dispose()
        {
            // no shared resources yet, but there probably will be
        }

        public SKImage Render(Layout.Diagram diagram)
        {
            using var state = new RenderState();

            foreach (var line in diagram.Lines)
            {
                state.Lines[line] = state.MeasureLine(line);
            }

            foreach (var label in diagram.Labels)
            {
                state.Labels[label] = state.MeasureLabel(label);
            }

            foreach (var shape in diagram.Shapes)
            {
                state.Shapes[shape] = state.MeasureShape(shape);
            }

            var d = state.MeasureDiagram(diagram);

            var info = new SKImageInfo(d.PixelWidth, d.PixelHeight);
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
