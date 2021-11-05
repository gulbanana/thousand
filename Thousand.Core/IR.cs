using System;
using System.Collections.Generic;
using System.Linq;
using Thousand.Model;

// Intermediate representation shared between Canonicalise and Compose stages
namespace Thousand.IR
{
    public record Axes<T>(T Columns, T Rows)
    {
        public Axes(T both) : this(both, both) { }
        public Axes<U> Select<U>(Func<T, U> f) => new Axes<U>(f(Columns), f(Rows));
    }

    public record Config(decimal Scale, Colour? Fill, FlowKind GridFlow, int GridMax /* 0 = no max :/ */, Border Padding, Axes<decimal> Gutter, Axes<TrackSize> Layout, Axes<AlignmentKind> Alignment)
    {
        public Config() : this(1.0m, null, FlowKind.Auto, 0, new(0), new(0), new(new PackedSize()), new(AlignmentKind.Start)) { }
    }

    // the diagram root is a Region, which contains Entities - Nodes and Edges
    public abstract record Entity;
    public record Region(Config Config, IReadOnlyList<Entity> Entities)
    {
        // for tests
        public IReadOnlyList<Node> Nodes => Entities.OfType<Node>().ToList();
        public IReadOnlyList<Edge> Edges => Entities.OfType<Edge>().ToList();
    }

    // Objects may contain sub-regions
    public record StyledText(Font Font, string Content, AlignmentKind Justification);
    public record Node
    (
        Name Name, // this is a display name, not a unique identifier - it may have been derived from the classlist
        Region Region,
        StyledText? Label,
        Shape? Shape, 
        Stroke Stroke, // could be nullable instead of having NoWidth
        // layout
        Axes<AlignmentKind?> Alignment, Border Margin, decimal? MinWidth, decimal? MinHeight,
        int? Row, int? Column, Point? Position, CompassKind? Anchor, Point? Offset
        
    ) : Entity
    {
        public Node(Name name, Config config, params Entity[] children) : this(name, new Region(config, children), null, new Shape(ShapeKind.Rectangle, 0), new Stroke(), new Axes<AlignmentKind?>(null), new(0), null, null, null, null, null, null, null) { }
    }
    
    // there may be many IR.Edge for a single AST.Line
    public record Endpoint(Name Name, Node Target, MarkerKind? Marker, Anchor Anchor, Point Offset);
    public record Edge(Endpoint From, Endpoint To, Stroke Stroke, StyledText? Label) : Entity;
}
