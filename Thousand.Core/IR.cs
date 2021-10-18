using System;
using System.Collections.Generic;
using Thousand.Model;

// Intermediate representation shared between Canonicalise and Compose stages
namespace Thousand.IR
{
    public record Axes<T>(T Columns, T Rows)
    {
        public Axes(T both) : this(both, both) { }

        public Axes<U> Select<U>(Func<T, U> f) => new Axes<U>(f(Columns), f(Rows));
    }

    public record StyledText(Font Font, string Content, AlignmentKind Justification)
    {
        public StyledText(string content) : this(new Font(), content, AlignmentKind.Center) {  }
    }

    public record Config(Colour? Fill, FlowKind GridFlow, int GridMax /* 0 = no max :/ */, Border Padding, Axes<int> Gutter, Axes<TrackSize> Layout, Axes<AlignmentKind> Alignment)
    {
        public Config() : this(null, FlowKind.Columns, 0, new(0), new(0), new(new PackedSize()), new(AlignmentKind.Center)) { }
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
        Parse.Identifier Name, // used only for diagnostics
        Region Region,
        StyledText? Label,
        // layout
        Axes<AlignmentKind?> Alignment, Border Margin, decimal? MinWidth, decimal? MinHeight,
        int? Row, int? Column, Point? Position, CompassKind? Anchor, Point? Offset,
        // shape
        ShapeKind? Shape, int CornerRadius, Stroke Stroke
    )
    {
        public Object(string label, params Object[] children) : this(new Parse.Identifier("object"), new Region(new Config(), children), new StyledText(label), new Axes<AlignmentKind?>(null), new(0), null, null, null, null, null, null, null, ShapeKind.Rectangle, 0, new Stroke()) { }
        public Object(Config config, params Object[] children) : this(new Parse.Identifier("object"), new Region(config, children), null, new Axes<AlignmentKind?>(null), new(0), null, null, null, null, null, null, null, ShapeKind.Rectangle, 0, new Stroke()) { }
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
