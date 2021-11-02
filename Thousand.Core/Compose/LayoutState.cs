using System.Collections.Generic;
using Thousand.Model;

namespace Thousand.Compose
{
    internal sealed class LayoutState
    {
        public List<decimal> Columns { get; } = new();
        public List<decimal> Rows { get; } = new();
        public Dictionary<CompassKind, Point> Anchors { get; } = new();
        public Dictionary<IR.Node, NodeState> AllNodes { get; } = new(ReferenceEqualityComparer.Instance);
        public Dictionary<IR.Node, GridNodeState> GridNodes { get; } = new(ReferenceEqualityComparer.Instance);
        public Dictionary<IR.Node, AnchorNodeState> AnchorNodes { get; } = new(ReferenceEqualityComparer.Instance);
        public Dictionary<IR.Node, PositionNodeState> PositionNodes { get; } = new(ReferenceEqualityComparer.Instance);
    }
}
