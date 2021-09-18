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
            blueFill = new SKPaint { Color = SKColors.LightBlue, IsStroke = false, IsAntialias = true };
            redFill = new SKPaint { Color = SKColors.PaleVioletRed, IsStroke = false, IsAntialias = true };
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
            var info = new SKImageInfo(diagram.Width*2, diagram.Height*2);
            using var surface = SKSurface.Create(info);            
            
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.White);
            canvas.Scale(2f);

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

            var path = new SKPath();
            switch (shape.Kind)
            {
                case ShapeKind.Square:
                    path.AddRect(Pad(textBox, 4));
                    break;

                case ShapeKind.Oval: 
                    path.AddOval(Pad(textBox, 10));
                    break;

                case ShapeKind.Rounded:
                    path.AddRoundRect(new SKRoundRect(Pad(textBox, 4), 4));
                    break;
            };
            path.Close();

            return new(label.Center, path);
        }

        private void PaintShape(RenderState state, Layout.Shape shape)
        {
            var measures = state.Shapes[shape];

            var stroke = new SKPaint { Color = shape.Stroke.SK(), IsAntialias = true, IsStroke = true };
            var fill = new SKPaint { Color = shape.Fill.SK(), IsAntialias = true };

            state.Canvas.DrawPath(measures.Path, fill);
            state.Canvas.DrawPath(measures.Path, stroke);
        }

        private void PaintLine(RenderState state, Layout.Line line)
        {
            var from = state.Shapes[line.From];
            var to = state.Shapes[line.To];

            // used as fill or hairline, rather than stroke in the Skia sense
            var stroke = new SKPaint { Color = line.Stroke.SK(), IsAntialias = true };

            // for a non-hairline of width n, establish start points +n/2 and -n/2 perpendicular to the line
            var width = 1.0f;
            var unitPath = Normalize(to.Center - from.Center, width/2);
            var offset1 = SKMatrix.CreateRotationDegrees(-90).MapPoint(unitPath);
            var offset2 = SKMatrix.CreateRotationDegrees(90).MapPoint(unitPath);

            // draw a thin rectangle of the desired width
            var path = new SKPath();
            path.MoveTo(from.Center);
            path.LineTo(from.Center + offset1);
            path.LineTo(to.Center + offset1);
            path.LineTo(to.Center);
            path.LineTo(to.Center + offset2);
            path.LineTo(from.Center + offset2);
            path.Close();

            // subtract the rectangle regions within src/dst shapes 
            var visiblePath = path.Op(from.Path, SKPathOp.Difference).Op(to.Path, SKPathOp.Difference);

            // create the points for a hairline (and end cap positioning)
            var start = visiblePath.Points[0];
            var end = visiblePath.Points[3];
            if ((start - to.Center).LengthSquared < (start - from.Center).LengthSquared)
            {
                var swap = start;
                start = end;
                end = swap;
            }

            // draw the main line
            state.Canvas.DrawLine(start, end, stroke);

            // draw end cap
            var arrowhead = new SKPath();
            var arrowLength = Normalize(to.Center - from.Center, 7f);
            var arrowWidth = Normalize(to.Center - from.Center, 4f);
            var base1 = SKMatrix.CreateRotationDegrees(-90).MapPoint(arrowWidth);
            var base2 = SKMatrix.CreateRotationDegrees(90).MapPoint(arrowWidth);

            arrowhead.MoveTo(end - arrowLength);
            arrowhead.LineTo(end - arrowLength + base1);
            arrowhead.LineTo(end);
            arrowhead.LineTo(end - arrowLength + base2);
            arrowhead.Close();

            state.Canvas.DrawPath(arrowhead, stroke);
        }

        private static SKRect Pad(SKRect box, float padding)
        {
            return new SKRect(box.Left - padding, box.Top - padding, box.Right + padding, box.Bottom + padding);
        }

        private static SKPoint Normalize(SKPoint vector, float length = 1.0f)
        {
            return new SKPoint(vector.X / vector.Length * length, vector.Y / vector.Length * length);
        }
    }
}
