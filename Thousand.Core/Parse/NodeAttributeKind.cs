namespace Thousand.Parse
{
    // a "node" is two things: a positioned region, and possibly a drawn shape. in the high level syntax, both are defined by an "object".
    public enum NodeAttributeKind
    {
        Row,
        Col, Column,
        Width,
        Height,

        Shape,
        Padding,
        Corner, CornerRadius,
    }
}
