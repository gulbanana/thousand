using SkiaSharp;
using System;
using Thousand.Model;
using Topten.RichTextKit;

namespace Thousand.Render
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
            whiteFill.Dispose();
        }

        public SKImage Render(Layout.Diagram diagram)
        {
            var info = new SKImageInfo(diagram.Width, diagram.Height);
            using var surface = SKSurface.Create(info);            
            
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.White);

            var state = new RenderState(canvas);

            foreach (var label in diagram.Labels)
            {
                state.Labels[label] = MeasureLabel(label);
            }

            foreach (var shape in diagram.Shapes)
            {
                state.Shapes[shape] = MeasureShape(state, shape);
            }

            foreach (var shape in diagram.Shapes)
            {
                PaintShape(state, shape);
            }

            foreach (var label in diagram.Labels)
            {
                PaintLabel(state, label);
            }

            foreach (var line in diagram.Lines)
            {
                PaintLine(state, line);
            }

            return surface.Snapshot();
        }

        private PLabel MeasureLabel(Layout.Label label)
        {
            var text = new RichString()
                .FontFamily(SKTypeface.Default.FamilyName)
                .FontSize(20)
                .Alignment(TextAlignment.Center)
                .Add(label.Content);

            var center = new SKPoint(label.X, label.Y);
            var origin = center - new SKPoint(text.MeasuredWidth / 2, text.MeasuredHeight / 2);

            return new(text, center, origin);
        }

        private void PaintLabel(RenderState state, Layout.Label label)
        {
            var measures = state.Labels[label];
            measures.Text.Paint(state.Canvas, measures.Origin);
        }

        private PShape MeasureShape(RenderState state, Layout.Shape shape)
        {
            var label = state.Labels[shape.Fit];

            var textBox = new SKRect(label.Origin.X, label.Origin.Y, label.Origin.X + label.Text.MeasuredWidth, label.Origin.Y + label.Text.MeasuredHeight);
            var paddedBox = shape.Kind switch
            {
                ShapeKind.Square => Pad(textBox, 4),
                ShapeKind.Oval => Pad(textBox, 10),
                ShapeKind.Rounded => Pad(textBox, 4)
            };

            return new(label.Center, paddedBox);
        }

        private void PaintShape(RenderState state, Layout.Shape shape)
        {
            var measures = state.Shapes[shape];

            var stroke = new SKPaint { Color = shape.Stroke.SK(), IsAntialias = true, IsStroke = true };
            var fill = new SKPaint { Color = shape.Fill.SK(), IsAntialias = true };

            switch (shape.Kind)
            {
                case ShapeKind.Square:
                    state.Canvas.DrawRect(measures.Box, fill);
                    state.Canvas.DrawRect(measures.Box, stroke);
                    break;

                case ShapeKind.Oval:
                    state.Canvas.DrawOval(measures.Box, fill);
                    state.Canvas.DrawOval(measures.Box, stroke);
                    break;

                case ShapeKind.Rounded:
                    var paddedBox = new SKRoundRect(measures.Box, 4);
                    state.Canvas.DrawRoundRect(paddedBox, fill);
                    state.Canvas.DrawRoundRect(paddedBox, stroke);
                    break;
            }
        }

        // XXX better algorithm: calculate notional line between centers, find intersection with shapes, move start/end
        private void PaintLine(RenderState state, Layout.Line line)
        {
            var from = state.Shapes[line.From];
            var to = state.Shapes[line.To];

            var startX = 0f;
            var endX = 0f;
            if (from.Center.X < to.Center.X)
            {
                startX = from.Box.Right;
                endX = to.Box.Left;
            }
            else if (from.Center.X > to.Center.X)
            {
                startX = from.Box.Left;
                endX = to.Box.Right;
            }
            else
            {
                startX = from.Center.X;
                endX = to.Center.X;
            }

            var startY = 0f;
            var endY = 0f;
            if (from.Center.Y < to.Center.Y)
            {
                startY = from.Box.Bottom;
                endY = to.Box.Top;
            }
            else if (from.Center.Y > to.Center.Y)
            {
                startY = from.Box.Top;
                endY = to.Box.Bottom;
            }
            else
            {
                startY = from.Center.Y;
                endY = to.Center.Y;
            }

            var start = new SKPoint(startX, startY);
            var end = new SKPoint(endX, endY);

            var stroke = new SKPaint { Color = line.Stroke.SK(), IsAntialias = true };
            state.Canvas.DrawLine(start, end, stroke);

            // XXX does not angle arrow - always faces right
            var arrowhead = new SKPath();
            arrowhead.MoveTo(end);
            arrowhead.LineTo(end + new SKPoint(-7, -5));
            arrowhead.LineTo(end + new SKPoint(-7, 5));
            arrowhead.LineTo(end);

            state.Canvas.DrawPath(arrowhead, stroke);
        }

        private static SKRect Pad(SKRect box, float padding)
        {
            return new SKRect(box.Left - padding, box.Top - padding, box.Right + padding, box.Bottom + padding);
        }
    }
}
