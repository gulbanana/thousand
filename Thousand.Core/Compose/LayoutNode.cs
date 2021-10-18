﻿using Thousand.Model;

namespace Thousand.Compose
{
    internal record LayoutNode(Point Size, Border Margin);
    internal record GridLayoutNode(Point Size, Border Margin, int Row, int Column) : LayoutNode(Size, Margin);
    internal record AnchorLayoutNode(Point Size, Border Margin, CompassKind Anchor, IR.Axes<AlignmentKind> Alignment) : LayoutNode(Size, Margin);
    internal record PositionLayoutNode(Point Size, Border Margin, Point Origin) : LayoutNode(Size, Margin);
}
