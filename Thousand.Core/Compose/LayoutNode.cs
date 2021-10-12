using Thousand.Model;

namespace Thousand.Compose
{
    internal record LayoutNode(Point Size, Border Margin);
    internal record GridLayoutNode(Point Size, Border Margin, int Row, int Column) : LayoutNode(Size, Margin);
    internal record AnchorLayoutNode(Point Size, Border Margin, CompassKind Anchor) : LayoutNode(Size, Margin);

}
