namespace Thousand.Model
{
    public abstract record Width;
    public sealed record HairlineWidth : Width;
    public sealed record ZeroWidth: Width;
    public sealed record PositiveWidth(int Value): Width;
}
