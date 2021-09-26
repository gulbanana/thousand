using SkiaSharp;
using System;
using System.Collections.Generic;
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
                case ShapeKind.RoundRect:
                    path.AddRoundRect(new SKRoundRect(Pad(textBox, 5), 5));
                    break;

                case ShapeKind.Rectangle:
                    path.AddRect(Pad(textBox, 5));
                    break;

                case ShapeKind.Square:
                    path.AddRect(Square(Pad(textBox, 5)));
                    break;

                case ShapeKind.Oval:
                    path.AddOval(Pad(textBox, 10));
                    break;

                case ShapeKind.Circle:
                    path.AddCircle(shape.X, shape.Y, Pad(textBox, 10).Width/2);
                    break;

                case ShapeKind.Diamond:
                    path.AddPoly(Diamond(Pad(textBox, 10)));
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

            if (shape.StrokeWidth.HasValue)
            {
                stroke.StrokeWidth = shape.StrokeWidth.Value;
            }

            canvas.DrawPath(measures.Path, fill);
            canvas.DrawPath(measures.Path, stroke);
        }

        public void PaintLine(SKCanvas canvas, Layout.Line line)
        {
            var from = Shapes[line.From];
            var to = Shapes[line.To];

            // used as fill or hairline, rather than stroke in the Skia sense
            var stroke = new SKPaint { Color = line.Stroke.SK(), IsAntialias = false };
            var fill = new SKPaint { Color = line.Stroke.SK(), IsAntialias = true };

            // for a non-hairline of width n, establish start points +n/2 and -n/2 perpendicular to the line
            var unitPath = Normalize(to.Center - from.Center, line.Width/2 ?? 1f);
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

            // subtract the rectangle regions within src/dst shapes, producing a potentially complex thin region
            var visiblePath = path.Op(from.Path, SKPathOp.Difference).Op(to.Path, SKPathOp.Difference);
            if (visiblePath.PointCount == 0)
            {
                return; // XXX warning
            }

            // use the first and last line points within the drawn region as the points for a hairline (and end cap positioning)
            visiblePath.GetBounds(out var visibleBounds);
            var start = PointOnRect(visibleBounds, from.Center);
            var end = PointOnRect(visibleBounds, to.Center);

            // draw the main line
            if (line.Width.HasValue)
            {
                canvas.DrawPath(visiblePath, fill);
            }
            else // hairline
            {
                canvas.DrawLine(start, end, stroke);
            } 

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

            canvas.DrawPath(arrowhead, fill);
        }

        private static SKRect Pad(SKRect box, float padding)
        {
            return new SKRect(box.Left - padding, box.Top - padding, box.Right + padding, box.Bottom + padding);
        }

        private static SKPoint Normalize(SKPoint vector, float length = 1.0f)
        {
            return new SKPoint(vector.X / vector.Length * length, vector.Y / vector.Length * length);
        }

        private SKRect Square(SKRect box)
        {
            if (box.Width > box.Height)
            {
                var radius = box.Width / 2;
                return new SKRect(box.Left, box.MidY - radius, box.Right, box.MidY + radius);
            }
            else
            {
                var radius = box.Width / 2;
                return new SKRect(box.Left, box.MidY - radius, box.Right, box.MidY + radius);
            }
        }

        private SKPoint[] Diamond(SKRect box)
        {
            return new SKPoint[]
            {
                new(box.Left, box.MidY),
                new(box.MidX, box.Top),
                new(box.Right, box.MidY),
                new(box.MidX, box.Bottom),
            };
        }

        private static SKPoint PointOnRect(SKRect box, SKPoint vectorFromCenter)
        {
            var x = vectorFromCenter.X;
            var y = vectorFromCenter.Y;
            var minX = box.Left;
            var minY = box.Top;
            var maxX = box.Right;
            var maxY = box.Bottom;

            // if (midX - x == 0) -> m == ±Inf -> minYx/maxYx == x (because value / ±Inf = ±0)
            var midX = (minX + maxX) / 2;
            var midY = (minY + maxY) / 2;
            var m = (midY - y) / (midX - x);

            if (x <= midX)
            { 
                var minXy = m * (minX - x) + y;
                if (minY <= minXy && minXy <= maxY)
                {
                    return new(minX, minXy);
                }
            }

            if (x >= midX)
            { 
                var maxXy = m * (maxX - x) + y;
                if (minY <= maxXy && maxXy <= maxY)
                {
                    return new(maxX, maxXy);
                }
            }

            if (y <= midY)
            { 
                var minYx = (minY - y) / m + x;
                if (minX <= minYx && minYx <= maxX)
                {
                    return new(minYx, minY);
                }
            }

            if (y >= midY)
            { 
                var maxYx = (maxY - y) / m + x;
                if (minX <= maxYx && maxYx <= maxX)
                {
                    return new(maxYx, maxY);
                }
            }

            if (x == midX && y == midY)
            {
                return new(x, y);
            }

            throw new Exception("Cannot find intersection");
        }
    }
}
