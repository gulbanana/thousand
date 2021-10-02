using System;
using System.Collections.Generic;
using System.Linq;

namespace Thousand.Model
{
    public record Point(decimal X, decimal Y)
    {
        public static Point Zero { get; } = new(0, 0);

        public static Point operator +(Point a, Point b) => new(a.X + b.X, a.Y + b.Y);
        public static Point operator -(Point a, Point b) => new(a.X - b.X, a.Y - b.Y);
        public static Point operator /(Point a, int b) => new(a.X / b, a.Y / b);

        public double Length
        {
            get
            {
                var w = (double)Math.Abs(X);
                var h = (double)Math.Abs(Y);
                return Math.Sqrt(w * w + h * h);
            }
        }

        public Point Normalize(decimal length = 1m)
        {
            return new Point(X / (decimal)Length * length, Y / (decimal)Length * length);
        }

        public Point Pad(decimal padding) => new(X + padding * 2, Y + padding * 2);

        public Point Closest(IEnumerable<Point> points)
        {
            var closestPoint = points.First();
            var minDistance = (points.First() - this).Length;

            foreach (var p in points.Skip(1))
            {
                var distance = (p - this).Length;
                if (distance < minDistance)
                {
                    closestPoint = p;
                    minDistance = distance;                    
                }
            }

            return closestPoint;
        }
    }
}
