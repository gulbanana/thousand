namespace Thousand.Model
{
    public abstract record TrackSize;
    public sealed record PackedSize : TrackSize { public static TrackSize Instance { get; } = new PackedSize(); }
    public sealed record EqualContentSize: TrackSize { public static TrackSize Instance { get; } = new EqualContentSize(); }
    public sealed record EqualAreaSize : TrackSize { public static TrackSize Instance { get; } = new EqualAreaSize(); }
    public sealed record MinimumSize(int Value): TrackSize;
}
