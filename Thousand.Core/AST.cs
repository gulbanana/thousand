using Thousand.Model;

namespace Thousand.AST
{
    public abstract record Declaration;
    public record Node(string Label, NodeAttribute[] Attributes) : Declaration;
    public record Edge(string From, string To, EdgeAttribute[] Attributes) : Declaration;
    public record Document(Declaration[] Declarations);

    public abstract record NodeAttribute;
    public record NodeLabelAttribute(string Content) : NodeAttribute;
    public record NodeShapeAttribute(ShapeKind Kind) : NodeAttribute;
    public record NodeStrokeAttribute(Colour Colour) : NodeAttribute;
    public record NodeFillAttribute(Colour Colour) : NodeAttribute;
    public record NodeRowAttribute(int Value) : NodeAttribute;
    public record NodeColumnAttribute(int Value) : NodeAttribute;

    public abstract record EdgeAttribute;
    public record EdgeStrokeAttribute(Colour Colour) : EdgeAttribute;
}
