using SkiaSharp;
using System;

namespace Thousand
{
    public static class Renderer
    {
        public static SKImage Render(Layout.Diagram diagram)
        {
            var info = new SKImageInfo(diagram.Width, diagram.Height);
            using var surface = SKSurface.Create(info);            
            var canvas = surface.Canvas;

            var textBrush = new SKPaint { Color = SKColors.Red, TextAlign = SKTextAlign.Center, TextSize = 20 };
            var blueFill = new SKPaint { Color = SKColors.Blue };
            
            canvas.Clear(SKColors.White);
            foreach (var label in diagram.Labels)
            {
                var textBounds = new SKRect();
                textBrush.MeasureText("M", ref textBounds);

                canvas.DrawRect(new SKRect(label.X - 50, 40, label.X + 50, 60), blueFill);

                var lines = label.Content.Split(Environment.NewLine);
                var offset = textBounds.Height / 2 - (lines.Length - 1) * (textBrush.TextSize / 2);
                foreach (var line in lines)
                {
                    canvas.DrawText(line, new SKPoint(label.X, label.Y + offset), textBrush);
                    offset += textBrush.TextSize;
                }                
            }

            return surface.Snapshot();
        }
    }
}
