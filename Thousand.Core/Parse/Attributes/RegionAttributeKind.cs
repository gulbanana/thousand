namespace Thousand.Parse.Attributes
{
    public enum RegionAttributeKind
    {
        Fill,
        Padding,

        Grid,
        GridFlow,
        GridMax,

        Space, Gutter=Space,
        SpaceRows, GutterRows = SpaceRows,
        SpaceColumns, GutterColumns = SpaceColumns,

        Layout,
        LayoutRows,
        LayoutColumns,

        Justify,
        JustifyRows,
        JustifyColumns,
    }
}
