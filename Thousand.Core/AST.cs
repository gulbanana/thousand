using OneOf;
using Thousand.Model;

// Intermediate representation shared between Parse and Canonicalise stages
namespace Thousand.AST
{
    public abstract record DocumentAttribute;
    public record DocumentScaleAttribute(decimal Value) : DocumentAttribute;

    public abstract record TextAttribute;
    public record TextLabelAttribute(string? Content) : TextAttribute;
    public record TextFontFamilyAttribute(string Name) : TextAttribute;
    public record TextFontSizeAttribute(int Value) : TextAttribute;
    public record TextFontColourAttribute(Colour Colour) : TextAttribute;
    public record TextFontAttribute(string? Family, int? Size, Colour? Colour) : TextAttribute;

    public abstract record LineAttribute;
    public record LineStrokeColourAttribute(Colour Colour) : LineAttribute;
    public record LineStrokeStyleAttribute(StrokeKind Kind) : LineAttribute;
    public record LineStrokeWidthAttribute(Width Value) : LineAttribute;
    public record LineStrokeAttribute(Colour? Colour, StrokeKind? Style, Width? Width) : LineAttribute;

    public abstract record RegionAttribute;
    public record RegionFillAttribute(Colour? Colour) : RegionAttribute;
    public record RegionLayoutAttribute(LayoutKind Kind) : RegionAttribute;
    public record RegionPaddingAttribute(int Value) : RegionAttribute;
    public record RegionGutterAttribute(int Value) : RegionAttribute;

    public abstract record NodeAttribute;
    public record NodeRowAttribute(int Value) : NodeAttribute;
    public record NodeColumnAttribute(int Value) : NodeAttribute;    
    public record NodeWidthAttribute(int Value) : NodeAttribute;
    public record NodeHeightAttribute(int Value) : NodeAttribute;
    public record NodeShapeAttribute(ShapeKind? Kind) : NodeAttribute;
    public record NodeMarginAttribute(int Value) : NodeAttribute;
    public record NodeCornerRadiusAttribute(int Value) : NodeAttribute;

    public abstract record ArrowAttribute;
    public record ArrowAnchorStartAttribute(AnchorKind? Kind) : ArrowAttribute;
    public record ArrowAnchorEndAttribute(AnchorKind? Kind) : ArrowAttribute;
    public record ArrowAnchorAttribute(AnchorKind? Start, AnchorKind? End) : ArrowAttribute;
    public record ArrowOffsetStartXAttribute(int Offset) : ArrowAttribute;
    public record ArrowOffsetStartYAttribute(int Offset) : ArrowAttribute;
    public record ArrowOffsetEndXAttribute(int Offset) : ArrowAttribute;
    public record ArrowOffsetEndYAttribute(int Offset) : ArrowAttribute;
    public record ArrowOffsetXAttribute(int Start, int End) : ArrowAttribute;
    public record ArrowOffsetYAttribute(int Start, int End) : ArrowAttribute;

    [GenerateOneOf] public partial class ObjectAttribute : OneOfBase<NodeAttribute, RegionAttribute, LineAttribute, TextAttribute> { }
    [GenerateOneOf] public partial class ObjectDeclaration : OneOfBase<ObjectAttribute, TypedObject, TypedLine> { }

    [GenerateOneOf] public partial class SegmentAttribute : OneOfBase<ArrowAttribute, LineAttribute> { }
    
    [GenerateOneOf] public partial class DiagramAttribute : OneOfBase<DocumentAttribute, RegionAttribute> { }    
    [GenerateOneOf] public partial class DocumentDeclaration : OneOfBase<DiagramAttribute, Class, TypedObject, TypedLine> { }

    public abstract record Class(string Name, string[] BaseClasses);
    public record ObjectClass(string Name, string[] BaseClasses, ObjectAttribute[] Attributes) : Class(Name, BaseClasses);
    public record LineClass(string Name, string[] BaseClasses, SegmentAttribute[] Attributes) : Class(Name, BaseClasses);
    public record ObjectOrLineClass(string Name, string[] BaseClasses, LineAttribute[] Attributes) : Class(Name, BaseClasses);

    public record TypedObject(string[] Classes, string? Name, ObjectAttribute[] Attributes, ObjectDeclaration[] Children);
    public record LineSegment(string Target, ArrowKind? Direction);
    public record TypedLine(string[] Classes, LineSegment[] Segments, SegmentAttribute[] Attributes);
    public record Document(DocumentDeclaration[] Declarations);
}
