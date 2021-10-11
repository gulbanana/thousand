using System.Collections.Generic;
using Thousand.Model;

// Intermediate representation shared between Canonicalise and Compose stages
namespace Thousand.IR
{
    public record Axes<T>(T Columns, T Rows)
    {
        public Axes(T both) : this(both, both) { }
    }

    public record Config(Colour? Fill, LayoutKind Layout, FlowKind Flow, Border Padding, Axes<int> Gutter, Axes<TrackSize> Size, Axes<AlignmentKind> Alignment)
    {
        public Config() : this(null, LayoutKind.Grid, FlowKind.Row, new(0), new(0), new(new PackedSize()), new(AlignmentKind.Center)) { }
    }

    public record Region(Config Config, IReadOnlyList<Object> Objects)
    {
        public Region(Config config, params Object[] objects) : this(config, objects as IReadOnlyList<Object>) { }  
        public Region(params Object[] objects) : this(new Config(), objects as IReadOnlyList<Object>) { }
        public Region(Config config) : this(config, new Object[0]) { }

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
        
    public record Object
    (
        Region Region,
        // text
        string? Label,
        Font Font,
        // layout
        Axes<AlignmentKind?> Alignment, Border Margin, int? MinWidth, int? MinHeight,
        int? Row, int? Column, int? X, int? Y, CompassKind? Anchor, Point? Offset,
        // shape
        ShapeKind? Shape, int CornerRadius, Stroke Stroke
    )
    {
        public Object(string label, params Object[] children) : this(new Region(new Config(), children), label, new Font(), new Axes<AlignmentKind?>(null), new(0), null, null, null, null, null, null, null, null, ShapeKind.Rectangle, 0, new Stroke()) { }
        public Object(Config config, params Object[] children) : this(new Region(config, children), null, new Font(), new Axes<AlignmentKind?>(null), new(0), null, null, null, null, null, null, null, null, ShapeKind.Rectangle, 0, new Stroke()) { }
        public Object(params Object[] children) : this(new Config(), children) { }        
    }
    
    // there may be many IR.Edge for a single AST.Line
    public record Edge(Stroke Stroke, Parse.Identifier FromName, Parse.Identifier ToName, Object FromTarget, Object ToTarget, MarkerKind? FromMarker, MarkerKind? ToMarker, Anchor FromAnchor, Anchor ToAnchor, Point FromOffset, Point ToOffset)
    {
        public Edge(Object FromTarget, Object ToTarget) : this(new Stroke(), new Parse.Identifier("from"), new Parse.Identifier("to"), FromTarget, ToTarget, null, null, new NoAnchor(), new NoAnchor(), Point.Zero, Point.Zero) { }
    }
    
    public record Root(decimal Scale, Region Region, IReadOnlyList<Edge> Edges)
    {
        public Root(decimal scale, Region region, params Edge[] edges) : this(scale, region, edges as IReadOnlyList<Edge>) { }
        public Root(decimal scale, Region region) : this(scale, region, new Edge[0]) { }
        public Root(Region region, IReadOnlyList<Edge> edges) : this(1m, region, edges) { }
        public Root(Region region, params Edge[] edges) : this(1m, region, edges as IReadOnlyList<Edge>) { }
        public Root(Region region) : this(1m, region) { }

        public IEnumerable<Object> WalkObjects()
        {
            return Region.WalkObjects();
        }
    }
}
