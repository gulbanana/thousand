using System.Collections.Generic;
using Thousand.Model;

// Intermediate representation shared between Canonicalise and Compose stages
namespace Thousand.IR
{
    public record Axes<T>(T Columns, T Rows)
    {
        public Axes(T both) : this(both, both) { }
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

    public record Config(Colour? Fill, LayoutKind Layout, int Padding, Axes<int> Gutter, Axes<TrackSize> Size, Axes<AlignmentKind> Alignment)
    {
        public Config() : this(null, LayoutKind.Grid, 0, new(0), new(new PackedSize()), new(AlignmentKind.Center)) { }
    }
        
    public record Object
    (
        Region Region,
        // text
        string? Label,
        Font Font,
        // layout
        AlignmentKind? Alignment, int Margin, int? Row, int? Column, int? MinWidth, int? MinHeight,
        // shape
        ShapeKind? Shape, int CornerRadius, Stroke Stroke
    )
    {
        public Object(string label, params Object[] children) : this(new Region(new Config(), children), label, new Font(), null, 0, null, null, null, null, ShapeKind.Rectangle, 0, new Stroke()) { }
        public Object(Config config, params Object[] children) : this(new Region(config, children), null, new Font(), null, 0, null, null, null, null, ShapeKind.Rectangle, 0, new Stroke()) { }
        public Object(params Object[] children) : this(new Config(), children) { }        
    }
    
    // there may be many IR.Edge for a single AST.Line
    public record Edge(Stroke Stroke, Object FromTarget, Object ToTarget, MarkerKind? FromMarker, MarkerKind? ToMarker, AnchorKind? FromAnchor, AnchorKind? ToAnchor, Point FromOffset, Point ToOffset);
    
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
