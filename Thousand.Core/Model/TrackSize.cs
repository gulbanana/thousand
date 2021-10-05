namespace Thousand.Model
{
    public abstract record TrackSize;
    public sealed record PackedSize : TrackSize;
    public sealed record EqualSize: TrackSize;
    public sealed record FixedSize(int Value): TrackSize;
}
