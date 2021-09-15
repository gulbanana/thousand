using SkiaSharp;
using SkiaSharp.HarfBuzz;
using System;
using Topten.RichTextKit;

namespace Thousand
{
    public class Renderer : IDisposable
    {
        private readonly SKPaint blackStroke;
        private readonly SKPaint blueFill;
        private readonly SKPaint redFill;

        public Renderer()
        {
            blackStroke = new SKPaint { Color = SKColors.Black, IsStroke = true };
            blueFill = new SKPaint { Color = SKColors.LightBlue, IsStroke = false };
            redFill = new SKPaint { Color = SKColors.PaleVioletRed, IsStroke = false };
        }

        public void Dispose()
        {
            blackStroke.Dispose();
            blueFill.Dispose();
            redFill.Dispose();
        }

        public SKImage Render(Layout.Diagram diagram)
        {
            var info = new SKImageInfo(diagram.Width, diagram.Height);
            using var surface = SKSurface.Create(info);            
            
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.White);

            foreach (var label in diagram.Labels)
            {
                PaintLabel(canvas, label);
            }

            return surface.Snapshot();
        }

        private bool red = false;
        private void PaintLabel(SKCanvas canvas, Layout.Label label)
        {
            // set and measure text
            var block = new RichString()
                .FontFamily(SKTypeface.Default.FamilyName)
                .FontSize(20)
                .Alignment(TextAlignment.Center)
                .Add(label.Content);

            var center = new SKPoint(label.X, label.Y);
            var origin = center - new SKPoint(block.MeasuredWidth / 2, block.MeasuredHeight / 2);

            // block debug
            var fullBlock = new SKRect(label.X - Composer.W / 2, 0, label.X + Composer.W / 2, Composer.W);
            canvas.DrawRect(fullBlock, red ? redFill : blueFill);
            red = !red;

            // node box
            var paddedBox = new SKRect(origin.X - 2, origin.Y - 2, origin.X + block.MeasuredWidth + 2, origin.Y + block.MeasuredHeight + 2);
            canvas.DrawRect(paddedBox, blackStroke);

            // node label
            block.Paint(canvas, origin);
        }
    }
}
