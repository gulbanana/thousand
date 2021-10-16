namespace Thousand.Model
{
    public abstract record TrackSize;
    public sealed record PackedSize : TrackSize;
    public sealed record EqualContentSize: TrackSize;
    public sealed record EqualAreaSize : TrackSize;
    public sealed record MinimumSize(int Value): TrackSize;
}
