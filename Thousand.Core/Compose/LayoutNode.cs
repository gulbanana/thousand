using Thousand.Model;

namespace Thousand.Compose
{
    internal record LayoutNode(Point DesiredSize, Border Margin);
    internal record GridLayoutNode(Point DesiredSize, Border Margin, int Row, int Column) : LayoutNode(DesiredSize, Margin);
    internal record AnchorLayoutNode(Point DesiredSize, Border Margin, CompassKind Anchor) : LayoutNode(DesiredSize, Margin);

}
