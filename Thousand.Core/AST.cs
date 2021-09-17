namespace Thousand.AST
{
    public record Node(string Label, NodeAttribute[] Attributes);
    public record Document(Node[] Nodes);

    public abstract record NodeAttribute;
    public record NodeLabelAttribute(string Content) : NodeAttribute;
    public record NodeShapeAttribute(ShapeKind Kind) : NodeAttribute;
}
