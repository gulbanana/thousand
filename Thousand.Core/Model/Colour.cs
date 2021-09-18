namespace Thousand.Model
{
    public record Colour(byte R, byte G, byte B)
    {
        public static Colour Black { get; } = new Colour(0, 0, 0);
        public static Colour White { get; } = new Colour(255, 255, 255);
        public static Colour Red { get; } = new Colour(255, 0, 0);
        public static Colour Green { get; } = new Colour(0, 255, 0);
        public static Colour Blue { get; } = new Colour(0, 0, 255);
    }
}
