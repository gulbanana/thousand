using Thousand.Model;

namespace Thousand.Compose
{
    internal record NodeState(Point Size, Border Margin);
    internal record GridNodeState(Point Size, Border Margin, int Row, int Column) : NodeState(Size, Margin);
    internal record AnchorNodeState(Point Size, Border Margin, CompassKind Anchor, IR.Axes<AlignmentKind> Alignment) : NodeState(Size, Margin);
    internal record PositionNodeState(Point Size, Border Margin, Point Origin) : NodeState(Size, Margin);
    internal record ErrorNodeState() : NodeState(Point.Zero, Border.Zero);
}
