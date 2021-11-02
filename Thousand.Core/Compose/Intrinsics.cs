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
    internal static class Intrinsics
    {
        public static BlockMeasurements TextBlock(IR.StyledText text)
        {
            var style = new Style()
            {
                FontFamily = text.Font.Family,
                FontSize = text.Font.Size
            };

            var textBlock = new TextBlock()
            {
                Alignment = text.Justification switch {
                    AlignmentKind.Center or AlignmentKind.Stretch => TextAlignment.Center, // it's a shame RichTextKit doesn't do full justification 
                    AlignmentKind.Start => TextAlignment.Left,
                    AlignmentKind.End => TextAlignment.Right
                }
            };

            textBlock.AddText(text.Content, style);

            var pixelWidth = (decimal)textBlock.MeasuredWidth;
            var pixelHeight = (decimal)textBlock.MeasuredHeight;

            var lines = textBlock.Lines
                .Select(l => new LineMeasurements(
                    new Point((decimal)l.Runs[0].XCoord, (decimal)l.YCoord),
                    new Point((decimal)l.Width, (decimal)l.Height),
                    text.Content.Substring(l.Start, l.Length).TrimEnd('\n')))
                .ToArray();

            return new BlockMeasurements(new Point(pixelWidth, pixelHeight), lines);
        }

        public static (Point? from, Point? to) Line(Point fromPoint, Point toPoint, (Shape shape, Rect bounds)? fromShape, (Shape shape, Rect bounds)? toShape)
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
                var fromPath = SkiaPath.Create(fromShape.Value.shape, fromShape.Value.bounds);
                path = path.Op(fromPath, SKPathOp.Difference)??path;
            }
            if (toShape != null)
            {
                var toPath = SkiaPath.Create(toShape.Value.shape, toShape.Value.bounds);
                path = path.Op(toPath, SKPathOp.Difference)??path;
            }

            // use the intersection of the straight path with the drawable region's bounding box as the first and last points for the real line
            path.GetTightBounds(out var visibleBounds);

            return (PointOnRect(visibleBounds.KS(), fromPoint), PointOnRect(visibleBounds.KS(), toPoint));
        }

        private static Point? PointOnRect(Rect box, Point vectorFromCenter)
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

            return null;
        }
    }
}
