using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Thousand.Model;

namespace Thousand.Tests.Composition
{
    internal static class DSL
    {
        private static readonly Dictionary<string, Dictionary<string, IR.Node>> targets = new();

        public static IR.StyledText Label(string content) => new IR.StyledText(new Font(), content, AlignmentKind.Center);

        public static IR.Config Config() => new IR.Config();

        public static IR.Region Region(IR.Config config, params IR.Entity[] entities) => new IR.Region(config, entities);
        public static IR.Region Region(params IR.Entity[] entities) => new IR.Region(new IR.Config(), entities);

        public static IR.Node Node(string name, params IR.Entity[] children) => new IR.Node(new Name(name), new IR.Config(), children);
        public static IR.Node Node(IR.Config config, params IR.Entity[] children) => new IR.Node(new Name("object"), config, children);        
        public static IR.Node Node(params IR.Entity[] children) => new IR.Node(new Name("object"), new IR.Config(), children);

        public static IR.Endpoint Endpoint(IR.Node target) => new IR.Endpoint(target.Name, target, null, new NoAnchor(), Point.Zero);
        public static IR.Endpoint Endpoint(IR.Node target, Point offset) => new IR.Endpoint(target.Name, target, null, new NoAnchor(), offset);
        public static IR.Endpoint Endpoint(IR.Node target, Anchor anchor) => new IR.Endpoint(target.Name, target, null, anchor, Point.Zero);
        public static IR.Endpoint Endpoint(IR.Node target, CompassKind anchor) => new IR.Endpoint(target.Name, target, null, new SpecificAnchor(anchor), Point.Zero);
        public static IR.Endpoint Endpoint(string target, [CallerMemberName] string caller = null!) => Endpoint(targets[caller][target]);
        public static IR.Endpoint Endpoint(string target, CompassKind anchor, [CallerMemberName] string caller = null!) => Endpoint(targets[caller][target], anchor);

        public static IR.Edge Edge(IR.Endpoint from, IR.Endpoint to) => new IR.Edge(from, to, new Stroke(), null);
        public static IR.Edge Edge(IR.Node from, IR.Node to) => new IR.Edge(Endpoint(from), Endpoint(to), new Stroke(), null);
        public static IR.Edge Edge(string from, string to, [CallerMemberName] string caller = null!) => new IR.Edge(Endpoint(targets[caller][from]), Endpoint(targets[caller][to]), new Stroke(), null);

        public static IR.Node Target(string name, IR.Node node, [CallerMemberName] string caller = null!)
        {
            if (!targets.ContainsKey(caller))
            {
                targets[caller] = new();
            }
            targets[caller][name] = node;
            return node;
        }

        public static IR.Node Target(IR.Node node, [CallerMemberName] string caller = null!)
        {
            return Target(node.Name.AsKey, node, caller);
        }
    }
}
