namespace Thousand.Model
{
    public abstract record Anchor;
    public record NoAnchor() : Anchor { public static Anchor Instance { get; } = new NoAnchor(); }
    public record CornerAnchor() : Anchor { public static Anchor Instance { get; } = new CornerAnchor(); }
    public record AnyAnchor() : Anchor { public static Anchor Instance { get; } = new AnyAnchor(); }
    public record SpecificAnchor(CompassKind Kind) : Anchor;    
}
