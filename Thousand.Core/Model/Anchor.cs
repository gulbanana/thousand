namespace Thousand.Model
{
    public abstract record Anchor;
    public record NoAnchor() : Anchor;
    public record CornerAnchor() : Anchor;
    public record AnyAnchor() : Anchor;    
    public record SpecificAnchor(CompassKind Kind) : Anchor;    
}
