using System.Collections.Generic;
using Thousand.Model;

// Intermediate representation shared between Canonicalise and Compose stages
namespace Thousand.IR
{
    public record Region(LayoutKind Layout, int Margin, int Gutter)
    {
        public Region() : this(LayoutKind.Grid, 0, 20) { }
    }

    public record Config(decimal Scale, Colour? Background, Region Region)
    {
        public Config() : this(1, Colour.White, new Region()) { }
    }
        
    public record Object
    (
        string? Label,
        Font Font,
        Region Region,
        // layout
        int? Row, int? Column, int? Width, int? Height,
        // shape
        ShapeKind? Shape, int Padding, int CornerRadius, Stroke Stroke, Colour Fill
    )
    {
        public Object() : this(null, new Font(), new Region(), null, null, null, null, ShapeKind.RoundRectangle, 15, 5, new Stroke(), Colour.White) { }
        public Object(string label) : this(label, new Font(), new Region(), null, null, null, null, ShapeKind.RoundRectangle, 15, 5, new Stroke(), Colour.White) { }
    }
    
    public record Edge(Object FromTarget, Object ToTarget, AnchorKind? FromAnchor, AnchorKind? ToAnchor, Point FromOffset, Point ToOffset, Stroke Stroke);
    
    public record Rules(Config Config, IReadOnlyList<Object> Objects, IReadOnlyList<Edge> Edges);
}
