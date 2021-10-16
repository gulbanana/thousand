using System.Collections.Generic;
using Thousand.Model;

namespace Thousand.Compose
{
    internal sealed class LayoutState
    {
        public List<decimal> Columns { get; } = new();
        public List<decimal> Rows { get; } = new();
        public Dictionary<CompassKind, Point> Anchors { get; } = new();
        public Dictionary<IR.Object, LayoutNode> AllNodes { get; } = new(ReferenceEqualityComparer.Instance);
        public Dictionary<IR.Object, GridLayoutNode> GridNodes { get; } = new(ReferenceEqualityComparer.Instance);
        public Dictionary<IR.Object, AnchorLayoutNode> AnchorNodes { get; } = new(ReferenceEqualityComparer.Instance);
    }
}
