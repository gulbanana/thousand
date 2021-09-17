using SkiaSharp;
using System;
using Thousand.Model;
using Topten.RichTextKit;

namespace Thousand
{
    public class Renderer : IDisposable
    {
        private readonly SKPaint blackStroke;
        private readonly SKPaint blueFill;
        private readonly SKPaint redFill;
        private readonly SKPaint whiteFill;

        public Renderer()
        {
            blueFill = new SKPaint { Color = SKColors.LightBlue, IsStroke = false };
            redFill = new SKPaint { Color = SKColors.PaleVioletRed, IsStroke = false };
            whiteFill = new SKPaint { Color = SKColors.White, IsStroke = false, IsAntialias = true };
            blackStroke = new SKPaint { Color = SKColors.Black, IsStroke = true, IsAntialias = true };
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

            foreach (var line in diagram.Lines)
            {
                var linePaint = new SKPaint { Color = line.Stroke.SK(), IsAntialias = true };
                canvas.DrawLine(new SKPoint(line.X1, line.Y1), new SKPoint(line.X2, line.Y2), linePaint);
            }

            foreach (var shape in diagram.Shapes)
            {
                PaintShape(canvas, shape);
            }

            foreach (var label in diagram.Labels)
            {
                PaintLabel(canvas, label);
            }

            return surface.Snapshot();
        }

        private void PaintShape(SKCanvas canvas, Layout.Shape shape)
        {
            var block = new RichString()
                .FontFamily(SKTypeface.Default.FamilyName)
                .FontSize(20)
                .Alignment(TextAlignment.Center)
                .Add(shape.Fit.Content);

            var center = new SKPoint(shape.X, shape.Y);
            var origin = center - new SKPoint(block.MeasuredWidth / 2, block.MeasuredHeight / 2);

            var box = new SKRect(origin.X, origin.Y, origin.X + block.MeasuredWidth, origin.Y + block.MeasuredHeight);
            var stroke = new SKPaint { Color = shape.Stroke.SK(), IsAntialias = true, IsStroke = true };
            var fill = new SKPaint { Color = shape.Fill.SK(), IsAntialias = true };

            switch (shape.Kind)
            {
                case ShapeKind.Square:
                    var paddedBox = Pad(box, 4);
                    canvas.DrawRect(paddedBox, fill);
                    canvas.DrawRect(paddedBox, stroke);
                    break;

                case ShapeKind.Oval:
                    var paddedOval = Pad(box, 10);
                    canvas.DrawOval(paddedOval, fill);
                    canvas.DrawOval(paddedOval, stroke);
                    break;

                case ShapeKind.Rounded:
                    var paddedRect = new SKRoundRect(Pad(box, 4), 4);
                    canvas.DrawRoundRect(paddedRect, fill);
                    canvas.DrawRoundRect(paddedRect, stroke);
                    break;
            }            
        }

        private void PaintLabel(SKCanvas canvas, Layout.Label label)
        {
            var block = new RichString()
                .FontFamily(SKTypeface.Default.FamilyName)
                .FontSize(20)
                .Alignment(TextAlignment.Center)
                .Add(label.Content);

            var center = new SKPoint(label.X, label.Y);
            var origin = center - new SKPoint(block.MeasuredWidth / 2, block.MeasuredHeight / 2);

            block.Paint(canvas, origin);
        }

        private static SKRect Pad(SKRect box, float padding)
        {
            return new SKRect(box.Left - padding, box.Top - padding, box.Right + padding, box.Bottom + padding);
        }
    }
}
