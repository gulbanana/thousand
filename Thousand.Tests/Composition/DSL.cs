using Thousand.Model;

namespace Thousand.Tests.Composition
{
    internal static class DSL
    {
        public static IR.StyledText Label(string content) => new IR.StyledText(new Font(), content, AlignmentKind.Center);

        public static IR.Config Config() => new IR.Config();

        public static IR.Region Region(IR.Config config, params IR.Entity[] entities) => new IR.Region(config, entities);
        public static IR.Region Region(params IR.Entity[] entities) => new IR.Region(new IR.Config(), entities);

        public static IR.Node Object(IR.Config config, params IR.Entity[] children) => new IR.Node("object", config, children);
        public static IR.Node Object(string name, params IR.Entity[] children) => new IR.Node(name, new IR.Config(), children);
        public static IR.Node Object(params IR.Entity[] children) => new IR.Node("object", new IR.Config(), children);

        public static IR.Endpoint Endpoint(IR.Node target) => new IR.Endpoint(target.Name, target, null, new NoAnchor(), Point.Zero);
        public static IR.Endpoint Endpoint(IR.Node target, Point offset) => new IR.Endpoint(target.Name, target, null, new NoAnchor(), offset);
        public static IR.Endpoint Endpoint(IR.Node target, Anchor anchor) => new IR.Endpoint(target.Name, target, null, anchor, Point.Zero);
        public static IR.Endpoint Endpoint(IR.Node target, CompassKind anchor) => new IR.Endpoint(target.Name, target, null, new SpecificAnchor(anchor), Point.Zero);

        public static IR.Edge Edge(IR.Node from, IR.Node to) => new IR.Edge(Endpoint(from), Endpoint(to), new Stroke(), null);
        public static IR.Edge Edge(IR.Endpoint from, IR.Endpoint to) => new IR.Edge(from, to, new Stroke(), null);
    }
}
