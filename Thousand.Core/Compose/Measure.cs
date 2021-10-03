using System;
using System.Collections.Generic;
using SkiaSharp;
using Topten.RichTextKit;
using Thousand.Model;
using Thousand.Render;
using System.Linq;

namespace Thousand.Compose
{
    /// <summary>
    /// Calculates intrinsic sizes of text and lines using Skia/HarfBuzz facilities.
    /// </summary>
    /// <remarks>
    /// Used in the composition stage, with the output shared by all renderers; this
    /// means that Skia is the ground truth for text shaping, but externally-supplied
    /// shaping is what formats like SVG require of us anyway.
    /// </remarks>
    internal static class Measure
    {
        public static BlockMeasurements TextBlock(string label, Font font)
        {
            var style = new Style()
            {
                FontFamily = font.Family,
                FontSize = font.Size
            };

            var textBlock = new TextBlock()
            {
                Alignment = TextAlignment.Center
            };

            textBlock.AddText(label, style);

            var pixelWidth = (int)MathF.Ceiling(textBlock.MeasuredWidth);
            var pixelHeight = (int)MathF.Ceiling(textBlock.MeasuredHeight);

            // subpixel vertical positioning is not consistently supported in SVG
            if (pixelHeight % 2 != 0)
            {
                pixelHeight++;
            }

            var lines = textBlock.Lines
                .Select(l => new LineMeasurements(
                    new Point((decimal)l.Runs[0].XCoord, (decimal)Math.Ceiling(l.YCoord)), 
                    new Point((decimal)l.Width, (decimal)l.Height), 
                    label.Substring(l.Start, l.Length)))
                .ToArray();

            return new BlockMeasurements(new Point(pixelWidth, pixelHeight), lines);
        }

        public static (Point from, Point to) Line(Point fromPoint, Point toPoint, Layout.Shape? fromShape, Layout.Shape? toShape)
        {
            var start = fromPoint.SK();
            var end = toPoint.SK();

            // skia path intersection is area-based. raster a non-hairline, establishing start points +n/2 and -n/2 perpendicular to the line
            var unitPath = (toPoint - fromPoint).Normalize(.2m).SK();
            var offset1 = SKMatrix.CreateRotationDegrees(-90).MapPoint(unitPath);
            var offset2 = SKMatrix.CreateRotationDegrees(90).MapPoint(unitPath);

            // draw a thin rectangle using the control points
            var path = new SKPath();
            path.MoveTo((start + unitPath) - offset1);
            path.LineTo((end - unitPath) - offset1);
            path.LineTo((end - unitPath) - offset2);
            path.LineTo((start + unitPath) - offset2);
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

            return (PointOnRect(visibleBounds.KS(), fromPoint), PointOnRect(visibleBounds.KS(), toPoint));
        }

        public static IReadOnlyList<Point> Corners(Rect box)
        {
            var points = new Point[4];

            points[0] = new Point(box.Left, box.Top);
            points[1] = new Point(box.Right, box.Top);
            points[2] = new Point(box.Right, box.Bottom);
            points[3] = new Point(box.Left, box.Bottom);

            return points;
        }

        public static IReadOnlyList<Point> Corners(Layout.Shape shape)
        {
            var points = new Point[4];

            var nw = new Point(shape.Bounds.Left - 10, shape.Bounds.Top - 10);
            var ne = new Point(shape.Bounds.Right + 10, shape.Bounds.Top - 10);
            var se = new Point(shape.Bounds.Right + 10, shape.Bounds.Bottom + 10);
            var sw = new Point(shape.Bounds.Left - 10, shape.Bounds.Bottom + 10);            

            points[0] = Line(shape.Bounds.Center, nw, shape, null).from;
            points[1] = Line(shape.Bounds.Center, ne, shape, null).from;
            points[2] = Line(shape.Bounds.Center, se, shape, null).from;
            points[3] = Line(shape.Bounds.Center, sw, shape, null).from;

            return points;
        }

        private static Point PointOnRect(Rect box, Point vectorFromCenter)
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
            var m = midX == x ? 0 : (midY - y) / (midX - x);

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
                var minYx = m == 0 ? x : (minY - y) / m + x;
                if (minX <= minYx && minYx <= maxX)
                {
                    return new(minYx, minY);
                }
            }

            if (y >= midY)
            {
                var maxYx = m == 0 ? x : (maxY - y) / m + x;
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
