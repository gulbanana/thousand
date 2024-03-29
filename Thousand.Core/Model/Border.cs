﻿using System;

namespace Thousand.Model
{
    public record Border(decimal Left, decimal Top, decimal Right, decimal Bottom)
    {
        public static Border Zero { get; } = new Border(0);

        public Border(decimal uniform) : this(uniform, uniform, uniform, uniform) { }
        public Border(decimal x, decimal y) : this(x, y, x, y) { }

        public decimal X => Left + Right;
        public decimal Y => Top + Bottom;
        public Point TopLeft => new Point(Left, Top);

        public static Border operator /(Border a, decimal b) => new(a.Left / b, a.Top / b, a.Right / b, a.Bottom / b);

        public Border Combine(Border that) => new(
            Math.Max(this.Left, that.Left),
            Math.Max(this.Top, that.Top),
            Math.Max(this.Right, that.Right),
            Math.Max(this.Bottom, that.Bottom)
        );
    }
}
