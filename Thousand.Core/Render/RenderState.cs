using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using Thousand.Model;
using Topten.RichTextKit;

namespace Thousand.Render
{
    internal class RenderState : IDisposable
    {
        public readonly Dictionary<Layout.Label, PLabel> Labels;
        public readonly Dictionary<Layout.Shape, PShape> Shapes;

        public RenderState()
        {
            Labels = new();
            Shapes = new();
        }

        public void Dispose()
        {
        }

        public PDiagram MeasureDiagram(Layout.Diagram diagram)
        {
            return new(
                (int)(diagram.Width * diagram.Scale),
                (int)(diagram.Height * diagram.Scale)
            );
        }

        public void PaintDiagram(SKCanvas canvas, Layout.Diagram diagram)
        {
            canvas.Scale(diagram.Scale);
            canvas.Clear(diagram.Background.SK());
        }

        public PLabel MeasureLabel(Layout.Label label)
        {
            var text = new RichString()
                .FontFamily(SKTypeface.Default.FamilyName)
                .FontSize(label.FontSize)
                .Alignment(TextAlignment.Center)
                .Add(label.Content);

            var center = new SKPoint(label.X, label.Y);
            var origin = center - new SKPoint(text.MeasuredWidth / 2, text.MeasuredHeight / 2);

            return new(text, center, origin);
        }

        public void PaintLabel(SKCanvas canvas, Layout.Label label)
        {
            var measures = Labels[label];
            measures.Text.Paint(canvas, measures.Origin);
        }

        public PShape MeasureShape(Layout.Shape shape)
        {
            if (shape.Fit == null)
            {
                throw new NotSupportedException("shape has no label");
            }

            var label = Labels[shape.Fit];

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

        public void PaintShape(SKCanvas canvas, Layout.Shape shape)
        {
            var measures = Shapes[shape];

            var stroke = new SKPaint { Color = shape.Stroke.SK(), IsAntialias = true, IsStroke = true };
            var fill = new SKPaint { Color = shape.Fill.SK(), IsAntialias = true };

            canvas.DrawPath(measures.Path, fill);
            canvas.DrawPath(measures.Path, stroke);
        }

        public void PaintLine(SKCanvas canvas, Layout.Line line)
        {
            var from = Shapes[line.From];
            var to = Shapes[line.To];

            // used as fill or hairline, rather than stroke in the Skia sense
            var stroke = new SKPaint { Color = line.Stroke.SK(), IsAntialias = true };

            // for a non-hairline of width n, establish start points +n/2 and -n/2 perpendicular to the line
            var width = 1.0f;
            var unitPath = Normalize(to.Center - from.Center, width / 2);
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
            if (visiblePath.PointCount == 0)
            {
                return; // XXX warning
            }

            // create the points for a hairline (and end cap positioning)
            var start = visiblePath.Points[0];
            var end = visiblePath.Points[visiblePath.PointCount - 3];
            if ((start - to.Center).LengthSquared < (start - from.Center).LengthSquared)
            {
                var swap = start;
                start = end;
                end = swap;
            }

            // draw the main line
            canvas.DrawLine(start, end, stroke);

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

            canvas.DrawPath(arrowhead, stroke);
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
