using SkiaSharp;
using SkiaSharp.HarfBuzz;
using System;
using Topten.RichTextKit;

namespace Thousand
{
    public static class Renderer
    {
        public static SKImage Render(Layout.Diagram diagram)
        {
            using var blackStroke = new SKPaint { Color = SKColors.Black, IsStroke = true };
            using var blueFill = new SKPaint { Color = SKColors.LightBlue, IsStroke = false };
            using var redFill = new SKPaint { Color = SKColors.PaleVioletRed, IsStroke = false };
            using var textBrush = new SKPaint { Color = SKColors.Black, TextAlign = SKTextAlign.Center, TextSize = 20, Typeface = SKTypeface.Default };
            using var textShaper = new SKShaper(SKTypeface.Default);

            var info = new SKImageInfo(diagram.Width, diagram.Height);
            using var surface = SKSurface.Create(info);            
            
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.White);

            var red = false;
            foreach (var label in diagram.Labels)
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

            return surface.Snapshot();
        }
    }
}
