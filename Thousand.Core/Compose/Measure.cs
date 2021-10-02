using SkiaSharp;
using System;
using Thousand.Model;
using Thousand.Render;

namespace Thousand.Compose
{
    public static class Measure
    {
        public static Point TextBlock(IR.Text t)
        {
            var text = new Topten.RichTextKit.RichString()
                .FontFamily(SKTypeface.Default.FamilyName)
                .FontSize(t.FontSize)
                .Alignment(Topten.RichTextKit.TextAlignment.Center)
                .Add(t.Label);

            return new((int)MathF.Ceiling(text.MeasuredWidth), (int)MathF.Ceiling(text.MeasuredHeight));
        }

        public static (Point from, Point to) Line(Rect fromBox, Rect toBox, Point fromOffset, Point toOffset, Layout.Shape? fromShape, Layout.Shape? toShape)
        {
            var fromPoint = (fromBox.Center() + fromOffset).SK();
            var toPoint = (toBox.Center() + toOffset).SK();

            // skia path intersection is area-based. start with a non-hairline, establishing start points +n/2 and -n/2 perpendicular to the line
            var unitPath = (toPoint - fromPoint).Normalize();
            var offset1 = SKMatrix.CreateRotationDegrees(-90).MapPoint(unitPath);
            var offset2 = SKMatrix.CreateRotationDegrees(90).MapPoint(unitPath);

            // draw a thin rectangle using the control points
            var path = new SKPath();
            path.MoveTo(fromPoint);
            path.LineTo(fromPoint + offset1);
            path.LineTo(toPoint + offset1);
            path.LineTo(toPoint);
            path.LineTo(toPoint + offset2);
            path.LineTo(fromPoint + offset2);
            path.Close();

            // subtract the rectangle regions within src/dst shapes, producing a potentially complex thin region
            if (fromShape != null)
            {
                var fromPath = SkiaPath.Create(fromShape);
                path = path.Op(fromPath, SKPathOp.Difference)??path;
            }
            if (toShape != null)
            {
                var toPath = SkiaPath.Create(toShape);
                path = path.Op(toPath, SKPathOp.Difference)??path;
            }

            // use the intersection of the straight path with the drawable region's bounding box as the first and last points for the real line
            path.GetTightBounds(out var visibleBounds);

            var start = PointOnRect(visibleBounds, fromPoint);
            var end = PointOnRect(visibleBounds, toPoint);

            return (new((int)start.X, (int)start.Y), new((int)end.X, (int)end.Y));
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
