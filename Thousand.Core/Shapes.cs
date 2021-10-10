using System;
using System.Collections.Generic;
using System.Linq;
using Thousand.Model;

namespace Thousand
{
    public static class Shapes
    {
        public static IReadOnlyDictionary<CompassKind, Connector> Anchors(ShapeKind shape, int cornerRadius, Rect bounds) => shape switch
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
                { CompassKind.NW, new(Point.ArcMidpoint(new(bounds.Left, bounds.Top+cornerRadius), new(bounds.Left+cornerRadius, bounds.Top)), true) },
                { CompassKind.NE, new(Point.ArcMidpoint(new(bounds.Right-cornerRadius, bounds.Top), new(bounds.Right, bounds.Top+cornerRadius)), true) },
                { CompassKind.SE, new(Point.ArcMidpoint(new(bounds.Right, bounds.Bottom-cornerRadius), new(bounds.Right-cornerRadius, bounds.Bottom)), true) },
                { CompassKind.SW, new(Point.ArcMidpoint(new(bounds.Left+cornerRadius, bounds.Bottom), new(bounds.Left, bounds.Bottom-cornerRadius)), true) },

                { CompassKind.N, new(bounds.Center.X, bounds.Top, false) },
                { CompassKind.E, new(bounds.Right, bounds.Center.Y, false) },
                { CompassKind.S, new(bounds.Center.X, bounds.Bottom, false) },
                { CompassKind.W, new(bounds.Left, bounds.Center.Y, false) },
            },

            ShapeKind.Ellipse or ShapeKind.Pill => new Dictionary<CompassKind, Connector>
            {
                { CompassKind.N, new(bounds.Center.X, bounds.Top, false) },
                { CompassKind.E, new(bounds.Right, bounds.Center.Y, false) },
                { CompassKind.S, new(bounds.Center.X, bounds.Bottom, false) },
                { CompassKind.W, new(bounds.Left, bounds.Center.Y, false) },
            },

            ShapeKind.Circle => CircleAnchors(bounds),

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
                { CompassKind.NW, new(bounds.Left + cornerRadius, bounds.Top, true) },
                { CompassKind.NE, new(bounds.Right - cornerRadius, bounds.Top, true) },
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

            ShapeKind.Octagon => new[] { CompassKind.NNW, CompassKind.NNE, CompassKind.ENE, CompassKind.ESE, CompassKind.SSE, CompassKind.SSW, CompassKind.WSW, CompassKind.WNW }
                .Zip(Octagon(bounds), Tuple.Create)
                .ToDictionary(t => t.Item1, t => new Connector(t.Item2, true)),

            _ => new Dictionary<CompassKind, Connector>()
        };

        public static decimal[] Dashes(StrokeKind style) => style switch
        {
            StrokeKind.ShortDash => new decimal[] { 3, 2 },
            StrokeKind.LongDash => new decimal[] { 6, 4 },
            StrokeKind.Dot => new decimal[] { 1, 2 },
            _ => throw new NotImplementedException($"unknown dash style {style}")
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

        // w = x + sqrt(2x^2)
        // x = -(1 - sqrt(2))w
        public static Point[] Octagon(Rect box)
        {
            var side = Math.Min(box.Width, box.Height);
            var third = -(1m - (decimal)Math.Sqrt(2.0)) * side;
            var c = (side - third) / 2m;

            return new Point[]
            {
                new(box.Left+c, box.Top),
                new(box.Right-c, box.Top),
                new(box.Right, box.Top+c),
                new(box.Right, box.Bottom-c),
                new(box.Right-c, box.Bottom),
                new(box.Left+c, box.Bottom),
                new(box.Left, box.Bottom-c),
                new(box.Left, box.Top+c)
            };
        }

        private static Dictionary<CompassKind, Connector> CircleAnchors(Rect bounds)
        {
            var n = new Point(bounds.Center.X, bounds.Top);
            var e = new Point(bounds.Right, bounds.Center.Y);
            var s = new Point(bounds.Center.X, bounds.Bottom);
            var w = new Point(bounds.Left, bounds.Center.Y);

            var ne = Point.ArcMidpoint(n, e, bounds.Center, bounds.Width/2);
            var nne = Point.ArcMidpoint(n, ne, bounds.Center, bounds.Width / 2);
            var ene = Point.ArcMidpoint(ne, e, bounds.Center, bounds.Width / 2);

            var se = Point.ArcMidpoint(e, s, bounds.Center, bounds.Width / 2);
            var ese = Point.ArcMidpoint(e, se, bounds.Center, bounds.Width / 2);
            var sse = Point.ArcMidpoint(se, s, bounds.Center, bounds.Width / 2);

            var sw = Point.ArcMidpoint(s, w, bounds.Center, bounds.Width / 2);
            var ssw = Point.ArcMidpoint(s, sw, bounds.Center, bounds.Width / 2);
            var wsw = Point.ArcMidpoint(sw, w, bounds.Center, bounds.Width / 2);

            var nw = Point.ArcMidpoint(w, n, bounds.Center, bounds.Width / 2);
            var wnw = Point.ArcMidpoint(w, nw, bounds.Center, bounds.Width / 2);
            var nnw = Point.ArcMidpoint(nw, n, bounds.Center, bounds.Width / 2);            

            return new()
            {
                { CompassKind.N, new(n, false) },
                { CompassKind.NNE, new(nne, false) },
                { CompassKind.NE, new(ne, false) },
                { CompassKind.ENE, new(ene, false) },
                { CompassKind.E, new(e, false) },
                { CompassKind.ESE, new(ese, false) },
                { CompassKind.SE, new(se, false) },
                { CompassKind.SSE, new(sse, false) },
                { CompassKind.S, new(s, false) },
                { CompassKind.SSW, new(ssw, false) },
                { CompassKind.SW, new(sw, false) },
                { CompassKind.WSW, new(wsw, false) },
                { CompassKind.W, new(w, false) },
                { CompassKind.WNW, new(wnw, false) },
                { CompassKind.NW, new(nw, false) },
                { CompassKind.NNW, new(nnw, false) },
            };
        }
    }
}
