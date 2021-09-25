using Thousand.Model;

// Intermediate representation shared between Parse and Compose stages
namespace Thousand.AST
{
    public abstract record DocumentDeclaration;
    public abstract record ScopeDeclaration : DocumentDeclaration;
    public record Class(string Name, string[] BaseClasses, NodeAttribute[] Attributes) : DocumentDeclaration;
    public record Node(string[] Classes, string? Name, string? Label, NodeAttribute[] Attributes, ScopeDeclaration[] Children) : ScopeDeclaration;
    public record Edge(string Target, ArrowKind? Direction);
    public record Edges(Edge[] Elements, LineAttribute[] Attributes) : ScopeDeclaration;
    public record Scope(ScopeDeclaration[] Declarations);
    public record Document(DocumentDeclaration[] Declarations);

    public abstract record RegionAttribute;

    public abstract record NodeAttribute : ScopeDeclaration;
    public record NodeLabelAttribute(string Content) : NodeAttribute;
    public record NodeShapeAttribute(ShapeKind Kind) : NodeAttribute;
    public record NodeStrokeAttribute(Colour Colour) : NodeAttribute;
    public record NodeFillAttribute(Colour Colour) : NodeAttribute;
    public record NodeRowAttribute(int Value) : NodeAttribute;
    public record NodeColumnAttribute(int Value) : NodeAttribute;
    public record NodeFontSizeAttribute(float Value) : NodeAttribute;

    public abstract record LineAttribute;
    public record LineStrokeAttribute(Colour Colour) : LineAttribute;
    public record LineWidthAttribute(float? Value) : LineAttribute;

    public abstract record DocumentAttribute : DocumentDeclaration;
    public record DocumentScaleAttribute(float Value) : DocumentAttribute;
    public record DocumentBackgroundAttribute(Colour Colour) : DocumentAttribute;
}
