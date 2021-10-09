namespace Thousand.Model
{
    public record Border(decimal Left, decimal Top, decimal Right, decimal Bottom)
    {
        public Border(decimal uniform) : this(uniform, uniform, uniform, uniform) { }
        public Border(decimal x, decimal y) : this(x, y, x, y) { }

        public decimal X => Left + Right;
        public decimal Y => Top + Bottom;
    }
}
