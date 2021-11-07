namespace Thousand.Model
{
    public abstract record TrackSize;
    public sealed record PackedSize : TrackSize { public static TrackSize Instance { get; } = new PackedSize(); }
    public sealed record EqualSize : TrackSize { public static TrackSize Instance { get; } = new EqualSize(); }
    public sealed record MinimumSize(decimal Value): TrackSize;
}
