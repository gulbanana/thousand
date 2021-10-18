namespace Thousand.Parse.Attributes
{
    // a "node" is two things: a positioned region, and possibly a drawn shape. in the high level syntax, both are defined by an "object".
    public enum NodeAttributeKind
    {
        Label,
        LabelContent,
        LabelJustify,

        Shape,
        CornerRadius, Corner = CornerRadius,

        Margin,
        AlignVertical, 
        AlignHorizontal,
        Align,        

        MinWidth,
        MinHeight,

        Row,
        Col, Column = Col,
        Position, Pos = Position,
    }
}
