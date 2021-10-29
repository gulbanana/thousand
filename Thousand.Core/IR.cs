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

    public record StyledText(Font Font, string Content, AlignmentKind Justification)
    {
        public StyledText(string content) : this(new Font(), content, AlignmentKind.Center) {  }
    }

    // the whole diagram is a Region, which contains Objects that also have Regions
    public record Region(Config Config, IReadOnlyList<Entity> Entities)
    {
        public Region(Config config, params Entity[] entities) : this(config, entities as IReadOnlyList<Entity>) { }
        public Region(params Entity[] entities) : this(new Config(), entities as IReadOnlyList<Entity>) { }
        public Region(Config config) : this(config, new Entity[0]) { }

        // for tests
        public IReadOnlyList<Object> Objects => Entities.OfType<Object>().ToList();
        public IReadOnlyList<Edge> Edges => Entities.OfType<Edge>().ToList();
        public IEnumerable<Object> WalkObjects()
        {
            foreach (var obj in Entities.OfType<Object>())
            {
                yield return obj;
                foreach (var child in obj.Region.WalkObjects())
                {
                    yield return child;
                }
            }
        }
    }

    public record Config(decimal Scale, Colour? Fill, FlowKind GridFlow, int GridMax /* 0 = no max :/ */, Border Padding, Axes<int> Gutter, Axes<TrackSize> Layout, Axes<AlignmentKind> Alignment)
    {
        public Config() : this(1.0m, null, FlowKind.Auto, 0, new(0), new(0), new(new PackedSize()), new(AlignmentKind.Start)) { }
    }

    public abstract record Entity;
        
    public record Object
    (
        Parse.Identifier Name, // this is a display name, not a unique identifier
        Region Region,
        StyledText? Label,
        // layout
        Axes<AlignmentKind?> Alignment, Border Margin, decimal? MinWidth, decimal? MinHeight,
        int? Row, int? Column, Point? Position, CompassKind? Anchor, Point? Offset,
        // shape
        ShapeKind? Shape, int CornerRadius, Stroke Stroke
    ) : Entity
    {
        public Object(string label, params Object[] children) : this(new Parse.Identifier("object"), new Region(new Config(), children), new StyledText(label), new Axes<AlignmentKind?>(null), new(0), null, null, null, null, null, null, null, ShapeKind.Rectangle, 0, new Stroke()) { }
        public Object(Config config, params Object[] children) : this(new Parse.Identifier("object"), new Region(config, children), null, new Axes<AlignmentKind?>(null), new(0), null, null, null, null, null, null, null, ShapeKind.Rectangle, 0, new Stroke()) { }
        public Object(params Object[] children) : this(new Config(), children) { }        
    }
    
    // there may be many IR.Edge for a single AST.Line
    public record Endpoint(Parse.Identifier Name, Object Target, MarkerKind? Marker, Anchor Anchor, Point Offset)
    {
        public Endpoint(Object target) : this(target.Name, target, null, new NoAnchor(), Point.Zero) { }
    }

    public record Edge(Endpoint From, Endpoint To, Stroke Stroke, StyledText? Label) : Entity
    {
        public Edge(Object from, Object to) : this(new Endpoint(from), new Endpoint(to), new Stroke(), null) { }
        public Edge(Endpoint from, Endpoint to) : this(from, to, new Stroke(), null) { }
    }
}
