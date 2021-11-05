namespace Thousand.Model
{
    public abstract record Width;
    public sealed record NoWidth : Width { public static Width Instance { get; } = new NoWidth(); }
    public sealed record HairlineWidth : Width { public static Width Instance { get; } = new HairlineWidth(); }
    public sealed record PositiveWidth(int Value): Width;
}
