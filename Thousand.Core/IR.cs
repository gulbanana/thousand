using System.Collections.Generic;
using Thousand.Model;

// Intermediate representation shared between Canonicalise and Compose stages
namespace Thousand.IR
{
    public record Region(Config Config, IReadOnlyList<Object> Objects)
    {
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

    public record Config(Colour? Fill, LayoutKind Layout, int Padding, int Gutter, TrackSize RowHeight, TrackSize ColumnWidth)
    {
        public Config() : this(null, LayoutKind.Grid, 0, 0, new PackedSize(), new PackedSize()) { }
    }
        
    public record Object
    (
        Region Region,
        // text
        string? Label,
        Font Font,
        // layout
        int Margin, int? Row, int? Column, int? MinWidth, int? MinHeight,
        // shape
        ShapeKind? Shape, int CornerRadius, Stroke Stroke
    )
    {
        public Object(params Object[] children) : this(new Region(new Config(null, LayoutKind.Grid, 15, 0, new PackedSize(), new PackedSize()), children), null, new Font(), 0, null, null, null, null, ShapeKind.Roundrect, 5, new Stroke()) { }
        public Object(string label, params Object[] children) : this(new Region(new Config(null, LayoutKind.Grid, 15, 0, new PackedSize(), new PackedSize()), children), label, new Font(), 0, null, null, null, null, ShapeKind.Roundrect, 5, new Stroke()) { }
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
