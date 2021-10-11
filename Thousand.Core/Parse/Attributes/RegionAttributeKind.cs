namespace Thousand.Parse.Attributes
{
    public enum RegionAttributeKind
    {
        Fill,
        Padding,
        Layout,

        Grid,
        GridFlow,
        GridMax,

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
