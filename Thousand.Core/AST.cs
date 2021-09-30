using OneOf;
using Thousand.Model;

// Intermediate representation shared between Parse and Canonicalise stages
namespace Thousand.AST
{
    public abstract record DocumentAttribute;
    public record DocumentScaleAttribute(float Value) : DocumentAttribute;

    public abstract record TextAttribute;
    public record TextLabelAttribute(string? Content) : TextAttribute;
    public record TextFontSizeAttribute(int Value) : TextAttribute;

    public abstract record StrokeAttribute;
    public record StrokeColourAttribute(Colour Colour) : StrokeAttribute;
    public record StrokeStyleAttribute(StrokeKind Kind) : StrokeAttribute;
    public record StrokeWidthAttribute(Width Value) : StrokeAttribute;
    public record StrokeShorthandAttribute(Colour? Colour, StrokeKind? Style, Width? Width) : StrokeAttribute;

    public abstract record RegionAttribute;
    public record RegionFillAttribute(Colour Colour) : RegionAttribute;
    public record RegionLayoutAttribute(LayoutKind Kind) : RegionAttribute;
    public record RegionMarginAttribute(int Value) : RegionAttribute;
    public record RegionGutterAttribute(int Value) : RegionAttribute;

    public abstract record NodeAttribute;
    public record NodeRowAttribute(int Value) : NodeAttribute;
    public record NodeColumnAttribute(int Value) : NodeAttribute;    
    public record NodeWidthAttribute(int Value) : NodeAttribute;
    public record NodeHeightAttribute(int Value) : NodeAttribute;
    public record NodeShapeAttribute(ShapeKind? Kind) : NodeAttribute;
    public record NodePaddingAttribute(int Value) : NodeAttribute;
    public record NodeCornerRadiusAttribute(int Value) : NodeAttribute;

    public abstract record ArrowAttribute;
    public record ArrowOffsetStartXAttribute(int Offset) : ArrowAttribute;
    public record ArrowOffsetStartYAttribute(int Offset) : ArrowAttribute;
    public record ArrowOffsetEndXAttribute(int Offset) : ArrowAttribute;
    public record ArrowOffsetEndYAttribute(int Offset) : ArrowAttribute;
    public record ArrowOffsetXAttribute(int Offset) : ArrowAttribute;
    public record ArrowOffsetYAttribute(int Offset) : ArrowAttribute;

    [GenerateOneOf] public partial class ObjectAttribute : OneOfBase<NodeAttribute, RegionAttribute, StrokeAttribute, TextAttribute> { }
    [GenerateOneOf] public partial class ObjectDeclaration : OneOfBase<ObjectAttribute, TypedObject, TypedLine> { }

    [GenerateOneOf] public partial class LineAttribute : OneOfBase<ArrowAttribute, StrokeAttribute> { }
    
    [GenerateOneOf] public partial class DiagramAttribute : OneOfBase<DocumentAttribute, RegionAttribute> { }    
    [GenerateOneOf] public partial class DocumentDeclaration : OneOfBase<DiagramAttribute, Class, TypedObject, TypedLine> { }

    public abstract record Class(string Name, string[] BaseClasses);
    public record ObjectClass(string Name, string[] BaseClasses, ObjectAttribute[] Attributes) : Class(Name, BaseClasses);
    public record LineClass(string Name, string[] BaseClasses, LineAttribute[] Attributes) : Class(Name, BaseClasses);
    public record ObjectOrLineClass(string Name, string[] BaseClasses, StrokeAttribute[] Attributes) : Class(Name, BaseClasses);

    public record TypedObject(string[] Classes, string? Name, ObjectAttribute[] Attributes, ObjectDeclaration[] Children);
    public record Edge(string Target, ArrowKind? Direction);
    public record TypedLine(string[] Classes, Edge[] Elements, LineAttribute[] Attributes);
    public record Document(DocumentDeclaration[] Declarations);
}
