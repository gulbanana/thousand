using OneOf;
using Thousand.Model;

// Intermediate representation shared between Parse and Canonicalise stages
namespace Thousand.AST
{
    public abstract record DocumentAttribute;
    public record DocumentScaleAttribute(float Value) : DocumentAttribute;

    public abstract record TextAttribute;
    public record TextLabelAttribute(string Content) : TextAttribute;
    public record TextFontSizeAttribute(int Value) : TextAttribute;

    public abstract record LineAttribute;
    public record LineStrokeAttribute(Colour Colour) : LineAttribute;
    public record LineWidthAttribute(int? Value) : LineAttribute;

    public abstract record RegionAttribute;
    public record RegionFillAttribute(Colour Colour) : RegionAttribute;

    public abstract record NodeAttribute;
    public record NodeShapeAttribute(ShapeKind Kind) : NodeAttribute;
    public record NodeRowAttribute(int Value) : NodeAttribute;
    public record NodeColumnAttribute(int Value) : NodeAttribute;

    public abstract record ArrowAttribute;
    public record ArrowOffsetStartAttribute(Point Offset) : ArrowAttribute;
    public record ArrowOffsetEndAttribute(Point Offset) : ArrowAttribute;
    public record ArrowOffsetBothAttribute(Point Offset) : ArrowAttribute;

    [GenerateOneOf] public partial class ObjectAttribute : OneOfBase<NodeAttribute, RegionAttribute, LineAttribute, TextAttribute> { }
    [GenerateOneOf] public partial class ObjectDeclaration : OneOfBase<ObjectAttribute, TypedObject, EdgeChain> { }

    [GenerateOneOf] public partial class EdgeAttribute : OneOfBase<ArrowAttribute, LineAttribute> { }

    [GenerateOneOf] public partial class DiagramAttribute : OneOfBase<DocumentAttribute, RegionAttribute> { }    
    [GenerateOneOf] public partial class DocumentDeclaration : OneOfBase<DiagramAttribute, Class, TypedObject, EdgeChain> { }

    public record Class(string Name, string[] BaseClasses, ObjectAttribute[] Attributes);
    public record TypedObject(string[] Classes, string? Name, string? Label, ObjectAttribute[] Attributes, ObjectDeclaration[] Children);
    public record Edge(string Target, ArrowKind? Direction);
    public record EdgeChain(Edge[] Elements, EdgeAttribute[] Attributes);
    public record Document(DocumentDeclaration[] Declarations);
}
