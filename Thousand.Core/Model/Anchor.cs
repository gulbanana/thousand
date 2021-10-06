namespace Thousand.Model
{
    public abstract record Anchor;
    public record NoAnchor() : Anchor;
    public record SpecificAnchor(AnchorKind Kind) : Anchor;
    public record ClosestAnchor(AnchorsKind Kind) : Anchor;
}
