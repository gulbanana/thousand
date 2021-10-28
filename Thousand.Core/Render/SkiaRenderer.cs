using SkiaSharp;
using System;
using Topten.RichTextKit;

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
            var info = new SKImageInfo(Math.Max(diagram.Width, 1), Math.Max(diagram.Height, 1));
            using var surface = SKSurface.Create(info);
            var state = new SkiaRenderState(surface.Canvas);

            state.ProcessCommandList(diagram.Commands);

            return surface.Snapshot();
        }
    }
}
