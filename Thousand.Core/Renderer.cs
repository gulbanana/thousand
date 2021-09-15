using SkiaSharp;
using SkiaSharp.HarfBuzz;
using System;

namespace Thousand
{
    public static class Renderer
    {
        public static SKImage Render(Layout.Diagram diagram)
        {
            using var blackStroke = new SKPaint { Color = SKColors.Black, IsStroke = true };
            using var textBrush = new SKPaint { Color = SKColors.Black, TextAlign = SKTextAlign.Center, TextSize = 20, Typeface = SKTypeface.Default };
            using var textShaper = new SKShaper(SKTypeface.Default);

            var info = new SKImageInfo(diagram.Width, diagram.Height);
            using var surface = SKSurface.Create(info);            
            
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.White);

            foreach (var label in diagram.Labels)
            {
                var mBounds = new SKRect();
                textBrush.MeasureText("M", ref mBounds);

                var lines = label.Content.Split(Environment.NewLine);

                var maxContentBounds = new SKRect();
                foreach (var line in lines)
                {
                    var contentBounds = new SKRect();
                    textBrush.MeasureText(line, ref contentBounds);
                    maxContentBounds.Union(contentBounds);
                }

                var yOffset = mBounds.Height / 2 - (lines.Length - 1) * (textBrush.TextSize / 2);
                maxContentBounds.Offset(label.X - maxContentBounds.Width / 2, label.Y + yOffset);

                foreach (var line in lines)
                {
                    canvas.DrawShapedText(line, new SKPoint(label.X - maxContentBounds.Width / 2, label.Y + yOffset), textBrush);
                    canvas.DrawText(line, new SKPoint(label.X, label.Y + yOffset), textBrush);
                    yOffset += textBrush.TextSize;
                }
                
                var paddedBounds = new SKRect(maxContentBounds.Left - 2, maxContentBounds.Top - 2, maxContentBounds.Right + 2, maxContentBounds.Bottom + (lines.Length - 1) * textBrush.TextSize + 2);
                canvas.DrawRect(paddedBounds, blackStroke);
            }

            return surface.Snapshot();
        }
    }
}
