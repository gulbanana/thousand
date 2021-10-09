﻿using System.Collections.Generic;

namespace Thousand.Model
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
    }
}
