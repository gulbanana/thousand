using SkiaSharp;
using System;
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

            // XXX block debug
            var red = false;
            foreach (var shape in diagram.Shapes)
            {
                var fullBlock = new SKRect(shape.X - Composer.W / 2, 0, shape.X + Composer.W / 2, Composer.W);
                canvas.DrawRect(fullBlock, red ? redFill : blueFill);
                red = !red;
            }
            // XXX block debug

            foreach (var line in diagram.Lines)
            {
                canvas.DrawLine(new SKPoint(line.X1, line.Y1), new SKPoint(line.X2, line.Y2), blackStroke);
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
            switch (shape.Kind)
            {
                case ShapeKind.Square:
                    var paddedBox = Pad(box, 4);
                    canvas.DrawRect(paddedBox, whiteFill);
                    canvas.DrawRect(paddedBox, blackStroke);
                    break;

                case ShapeKind.Oval:
                    var paddedOval = Pad(box, 10);
                    canvas.DrawOval(paddedOval, whiteFill);
                    canvas.DrawOval(paddedOval, blackStroke);
                    break;

                case ShapeKind.Rounded:
                    var paddedRect = new SKRoundRect(Pad(box, 4), 4);
                    canvas.DrawRoundRect(paddedRect, whiteFill);
                    canvas.DrawRoundRect(paddedRect, blackStroke);
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
