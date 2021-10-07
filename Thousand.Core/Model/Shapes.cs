using System;
using System.Collections.Generic;
using System.Linq;

namespace Thousand.Model
{
    internal static class Shapes
    {
        public static IReadOnlyDictionary<CompassKind, Connector> Anchors(ShapeKind shape, int corner, Rect bounds) => shape switch
        {
            ShapeKind.Square or ShapeKind.Rect => new Dictionary<CompassKind, Connector>
            {
                { CompassKind.NW, new(bounds.Left, bounds.Top, true) },
                { CompassKind.NE, new(bounds.Right, bounds.Top, true) },
                { CompassKind.SE, new(bounds.Right, bounds.Bottom, true) },
                { CompassKind.SW, new(bounds.Left, bounds.Bottom, true) },

                { CompassKind.N, new(bounds.Center.X, bounds.Top, false) },
                { CompassKind.E, new(bounds.Right, bounds.Center.Y, false) },
                { CompassKind.S, new(bounds.Center.X, bounds.Bottom, false) },
                { CompassKind.W, new(bounds.Left, bounds.Center.Y, false) },
            },
            
            ShapeKind.Roundsquare or ShapeKind.Roundrect => new Dictionary<CompassKind, Connector>
            {
                { CompassKind.NW, new(ArcMidpoint(new(bounds.Left, bounds.Top+corner), new(bounds.Left+corner, bounds.Top)), true) },
                { CompassKind.NE, new(ArcMidpoint(new(bounds.Right-corner, bounds.Top), new(bounds.Right, bounds.Top+corner)), true) },
                { CompassKind.SE, new(ArcMidpoint(new(bounds.Right, bounds.Bottom-corner), new(bounds.Right-corner, bounds.Bottom)), true) },
                { CompassKind.SW, new(ArcMidpoint(new(bounds.Left+corner, bounds.Bottom), new(bounds.Left, bounds.Bottom-corner)), true) },

                { CompassKind.N, new(bounds.Center.X, bounds.Top, false) },
                { CompassKind.E, new(bounds.Right, bounds.Center.Y, false) },
                { CompassKind.S, new(bounds.Center.X, bounds.Bottom, false) },
                { CompassKind.W, new(bounds.Left, bounds.Center.Y, false) },
            },

            ShapeKind.Ellipse or ShapeKind.Circle => new Dictionary<CompassKind, Connector>
            {
                { CompassKind.N, new(bounds.Center.X, bounds.Top, false) },
                { CompassKind.E, new(bounds.Right, bounds.Center.Y, false) },
                { CompassKind.S, new(bounds.Center.X, bounds.Bottom, false) },
                { CompassKind.W, new(bounds.Left, bounds.Center.Y, false) },
            },

            ShapeKind.Diamond or ShapeKind.Rhombus => new Dictionary<CompassKind, Connector>
            {
                { CompassKind.N, new(bounds.Center.X, bounds.Top, true) },
                { CompassKind.E, new(bounds.Right, bounds.Center.Y, true) },
                { CompassKind.S, new(bounds.Center.X, bounds.Bottom, true) },
                { CompassKind.W, new(bounds.Left, bounds.Center.Y, true) },

                { CompassKind.NW, new(bounds.Left + bounds.Width/4, bounds.Top + bounds.Height/4, false) },
                { CompassKind.NE, new(bounds.Right - bounds.Width/4, bounds.Top + bounds.Height/4, false) },
                { CompassKind.SE, new(bounds.Right - bounds.Width/4, bounds.Bottom - bounds.Height/4, false) },
                { CompassKind.SW, new(bounds.Left + bounds.Width/4, bounds.Bottom - bounds.Height/4, false) },
            },

            ShapeKind.Trapezium => new Dictionary<CompassKind, Connector>
            {
                { CompassKind.NW, new(bounds.Left + corner, bounds.Top, true) },
                { CompassKind.NE, new(bounds.Right - corner, bounds.Top, true) },
                { CompassKind.SE, new(bounds.Right, bounds.Bottom, true) },
                { CompassKind.SW, new(bounds.Left, bounds.Bottom, true) },

                { CompassKind.N, new(bounds.Center.X, bounds.Top, false) },
                { CompassKind.S, new(bounds.Center.X, bounds.Bottom, false) },
            },

            // XXX add the other sides as W and E, or remove S?
            ShapeKind.Triangle => new Dictionary<CompassKind, Connector>
            {
                { CompassKind.N, new(bounds.Center.X, bounds.Top, true) },
                { CompassKind.SE, new(bounds.Right, bounds.Bottom, true) },
                { CompassKind.SW, new(bounds.Left, bounds.Bottom, true) },

                { CompassKind.S, new(bounds.Center.X, bounds.Bottom, false) },
            },

            _ => new Dictionary<CompassKind, Connector>()
        };

        public static Point[] Diamond(Rect box) => new Point[]
        {
            new(box.Left, box.Center.Y),
            new(box.Center.X, box.Top),
            new(box.Right, box.Center.Y),
            new(box.Center.X, box.Bottom),
        };

        public static Point[] Triangle(Rect box) => new Point[]
        {
            new(box.Left, box.Bottom),
            new(box.Center.X, box.Top),
            new(box.Right, box.Bottom)
        };

        public static Point[] Trapezium(Rect box, int corner) => new Point[]
        {
            new(box.Left, box.Bottom),
            new(box.Left+corner, box.Top),
            new(box.Right-corner, box.Top),
            new(box.Right, box.Bottom)
        };

        // assumes clockwise quarter-circles 
        // y = +- sqrt(r^2 - (x-h)^2) + k
        private static Point ArcMidpoint(Point start, Point end)
        {
            if (start == end) return start;

            if (end.X > start.X && end.Y > start.Y)
            {
                var r = end.X - start.X;
                var cx = start.X;
                var cy = end.Y;

                var x = new[] { start.X, end.X }.Average();
                var radical = (r * r) - (x - cx) * (x - cx);
                var y = cy - (decimal)Math.Sqrt((double)radical);

                return new Point(x, y);
            }
            else if (end.X < start.X && end.Y > start.Y)
            {
                var r = start.X - end.X;
                var cx = end.X;
                var cy = start.Y;

                var x = new[] { start.X, end.X }.Average();
                var radical = (r * r) - (x - cx) * (x - cx);
                var y = cy + (decimal)Math.Sqrt((double)radical);

                return new Point(x, y);
            }
            else if (end.X > start.X && end.Y < start.Y)
            {
                var r = end.X - start.X;
                var cx = start.X;
                var cy = start.Y;

                var x = new[] { start.X, end.X }.Average();
                var radical = (r * r) - (x - cx) * (x - cx);
                var y = cy - (decimal)Math.Sqrt((double)radical);

                return new Point(x, y);
            }
            else if (end.X < start.X && end.Y < start.Y)
            {
                var r = start.X - end.X;
                var cx = end.X;
                var cy = end.Y;

                var x = new[] { start.X, end.X }.Average();
                var radical = (r * r) - (x - cx) * (x - cx);
                var y = cy + (decimal)Math.Sqrt((double)radical);

                return new Point(x, y);
            }
            else
            {
                return Point.Zero;
            }
        }
    }
}
