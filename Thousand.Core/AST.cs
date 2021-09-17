namespace Thousand.AST
{
    public abstract record Declaration;
    public record Node(string Label, NodeAttribute[] Attributes) : Declaration;
    public record Edge(string From, string To, EdgeAttribute[] Attributes) : Declaration;
    public record Document(Declaration[] Declarations);

    public abstract record NodeAttribute;
    public record NodeLabelAttribute(string Content) : NodeAttribute;
    public record NodeShapeAttribute(ShapeKind Kind) : NodeAttribute;

    public abstract record EdgeAttribute;
}
