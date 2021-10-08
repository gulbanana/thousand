namespace Thousand.Parse.Attributes
{
    public enum RegionAttributeKind
    {
        Fill,
        Padding,
        Layout,
        GridFlow,

        Space, Gutter=Space,
        SpaceRows, GutterRows = SpaceRows,
        SpaceColumns, GutterColumns = SpaceColumns,

        Pack,
        PackRows,
        PackColumns,

        Justify,
        JustifyRows,
        JustifyColumns,
    }
}
