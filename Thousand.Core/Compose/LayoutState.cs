using System.Collections.Generic;
using Thousand.Model;

namespace Thousand.Compose
{
    internal sealed class LayoutState
    {
        public List<decimal> Columns { get; } = new List<decimal>();
        public List<decimal> Rows { get; } = new List<decimal>();
        public Dictionary<CompassKind, Point> Anchors { get; } = new Dictionary<CompassKind, Point>();
        public Dictionary<IR.Object, GridLayoutNode> GridNodes { get; } = new Dictionary<IR.Object, GridLayoutNode>(ReferenceEqualityComparer.Instance);
        public Dictionary<IR.Object, LayoutNode> AllNodes { get; } = new Dictionary<IR.Object, LayoutNode>(ReferenceEqualityComparer.Instance);
    }
}
