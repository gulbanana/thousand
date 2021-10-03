using System.Collections.Generic;
using Thousand.Model;

// Intermediate representation shared between Canonicalise and Compose stages
namespace Thousand.IR
{
    public record Region(Config Config, IReadOnlyList<Object> Objects)
    {
        public IEnumerable<Object> WalkObjects()
        {
            foreach (var obj in Objects)
            {
                yield return obj;
                foreach (var child in obj.Region.WalkObjects())
                {
                    yield return child;
                }
            }
        }
    }

    public record Config(LayoutKind Layout, int Padding, int Gutter, Colour? Fill);
        
    public record Object
    (
        Region Region,
        // text
        string? Label,
        Font Font,
        // layout
        int Margin, int? Row, int? Column, int? Width, int? Height,
        // shape
        ShapeKind? Shape, int CornerRadius, Stroke Stroke
    )
    {
        public Object(params Object[] children) : this(new Region(new Config(LayoutKind.Grid, 20, 0, null), children), null, new Font(), 0, null, null, null, null, ShapeKind.RoundRectangle, 5, new Stroke()) { }
        public Object(string label, params Object[] children) : this(new Region(new Config(LayoutKind.Grid, 20, 0, null), children), label, new Font(), 0, null, null, null, null, ShapeKind.RoundRectangle, 5, new Stroke()) { }
    }
    
    // there may be many IR.Edge for a single AST.Line
    public record Edge(Stroke Stroke, Object FromTarget, Object ToTarget, MarkerKind? FromMarker, MarkerKind? ToMarker, AnchorKind? FromAnchor, AnchorKind? ToAnchor, Point FromOffset, Point ToOffset);
    
    public record Root(decimal Scale, Region Region, IReadOnlyList<Edge> Edges)
    {
        public IEnumerable<Object> WalkObjects()
        {
            return Region.WalkObjects();
        }
    }
}
