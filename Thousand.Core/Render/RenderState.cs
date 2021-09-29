using SkiaSharp;
using System;
using System.Collections.Generic;
using Thousand.Model;
using Topten.RichTextKit;

namespace Thousand.Render
{
    internal class RenderState : IDisposable
    {
        public readonly Dictionary<Layout.Shape, SKPath> ShapePaths;

        public RenderState()
        {
            ShapePaths = new();
        }

        public void Dispose()
        {
        }

        public void PaintDiagram(SKCanvas canvas, Layout.Diagram diagram)
        {
            canvas.Scale(diagram.Scale);
            canvas.Clear(diagram.Background.SK());
        }

        public void PaintLabel(SKCanvas canvas, Layout.Label label)
        {
            var text = new RichString()
                .FontFamily(SKTypeface.Default.FamilyName)
                .FontSize(label.FontSize)
                .Alignment(TextAlignment.Center)
                .Add(label.Content);

            text.Paint(canvas, label.Bounds.Origin().SK());
        }

        public SKPath MeasureShape(Layout.Shape shape)
        {
            var bounds = shape.Bounds.SK();

            var path = new SKPath();
            switch (shape.Kind)
            {
                case ShapeKind.RoundRect:
                case ShapeKind.RoundSquare:
                    path.AddRoundRect(new SKRoundRect(bounds, shape.CornerRadius));
                    break;

                case ShapeKind.Rectangle:
                case ShapeKind.Square:
                    path.AddRect(bounds);
                    break;

                case ShapeKind.Oval:
                    path.AddOval(bounds);
                    break;

                case ShapeKind.Circle:
                    path.AddCircle(bounds.MidX, bounds.MidY, bounds.Width/2);
                    break;

                case ShapeKind.Diamond:
                    path.AddPoly(Diamond(bounds));
                    break;
            };
            path.Close();

            return path;
        }

        public void PaintShape(SKCanvas canvas, Layout.Shape shape)
        {
            var path = ShapePaths[shape];

            var stroke = new SKPaint { Color = shape.Stroke.Colour.SK(), IsAntialias = true, IsStroke = true, PathEffect = StrokeEffect(shape.Stroke.Style) };
            var fill = new SKPaint { Color = shape.Fill.SK(), IsAntialias = true };

            if (shape.Stroke.Width.HasValue)
            {
                stroke.StrokeWidth = shape.Stroke.Width.Value;
            }

            canvas.DrawPath(path, fill);
            canvas.DrawPath(path, stroke);
        }

        public void PaintLine(SKCanvas canvas, Layout.Line line)
        {
            var fromPoint = line.Start.SK();
            var toPoint = line.End.SK();
            var fromPath = ShapePaths[line.From];
            var toPath = ShapePaths[line.To];

            var stroke = new SKPaint { Color = line.Stroke.Colour.SK(), IsAntialias = false, PathEffect = StrokeEffect(line.Stroke.Style) };
            var fill = new SKPaint { Color = line.Stroke.Colour.SK(), IsAntialias = true };

            // for a non-hairline of width n, establish start points +n/2 and -n/2 perpendicular to the line
            var unitPath = Normalize(toPoint - fromPoint, line.Stroke.Width ?? 1f);
            var offset1 = SKMatrix.CreateRotationDegrees(-90).MapPoint(unitPath);
            var offset2 = SKMatrix.CreateRotationDegrees(90).MapPoint(unitPath);

            // draw a thin rectangle of the desired width
            var path = new SKPath();
            path.MoveTo(fromPoint);
            path.LineTo(fromPoint + offset1);
            path.LineTo(toPoint + offset1);
            path.LineTo(toPoint);
            path.LineTo(toPoint + offset2);
            path.LineTo(fromPoint + offset2);
            path.Close();

            // subtract the rectangle regions within src/dst shapes, producing a potentially complex thin region
            var visiblePath = path.Op(fromPath, SKPathOp.Difference).Op(toPath, SKPathOp.Difference);
            if (visiblePath.PointCount == 0)
            {
                return; // XXX warning
            }

            // use the intersection of the straight path with the drawable region's bounding box as the first and last points for the real line
            visiblePath.GetBounds(out var visibleBounds);
            var start = PointOnRect(visibleBounds, fromPoint);
            var end = PointOnRect(visibleBounds, toPoint);

            // draw the main line
            if (line.Stroke.Width.HasValue)
            {
                stroke.StrokeWidth = line.Stroke.Width.Value;
            }

            canvas.DrawLine(start, end, stroke);

            // draw end cap
            var arrowhead = new SKPath();
            var arrowLength = Normalize(toPoint - fromPoint, 7f);
            var arrowWidth = Normalize(toPoint - fromPoint, 4f);
            var base1 = SKMatrix.CreateRotationDegrees(-90).MapPoint(arrowWidth);
            var base2 = SKMatrix.CreateRotationDegrees(90).MapPoint(arrowWidth);

            arrowhead.MoveTo(end - arrowLength);
            arrowhead.LineTo(end - arrowLength + base1);
            arrowhead.LineTo(end);
            arrowhead.LineTo(end - arrowLength + base2);
            arrowhead.Close();

            canvas.DrawPath(arrowhead, fill);
        }

        private static SKPoint Normalize(SKPoint vector, float length = 1.0f)
        {
            return new SKPoint(vector.X / vector.Length * length, vector.Y / vector.Length * length);
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

        private static SKPathEffect? StrokeEffect(StrokeKind style) => style switch
        {
            StrokeKind.Dashed => SKPathEffect.CreateDash(new[] { 3f, 2f }, 0f),
            _ => null
        };
    }
}
