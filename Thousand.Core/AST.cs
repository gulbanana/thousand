using OneOf;
using Thousand.Model;

// Intermediate representation shared between Parse and Canonicalise stages
namespace Thousand.AST
{
    /**********************************
     * Shared AST - mostly attributes *
     **********************************/
    public abstract record DocumentAttribute;
    public record DocumentScaleAttribute(decimal Value) : DocumentAttribute;

    public abstract record TextAttribute;
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
    public record RegionPaddingAttribute(Border Value) : RegionAttribute;
    public record RegionLayoutAttribute(LayoutKind Kind) : RegionAttribute;
    public record RegionGridFlowAttribute(FlowKind Kind) : RegionAttribute;

    public record RegionSpaceColumnsAttribute(int Value) : RegionAttribute;
    public record RegionSpaceRowsAttribute(int Value) : RegionAttribute;
    public record RegionSpaceAttribute(int Columns, int Rows) : RegionAttribute;

    public record RegionPackColumnsAttribute(TrackSize Size) : RegionAttribute;
    public record RegionPackRowsAttribute(TrackSize Size) : RegionAttribute;
    public record RegionPackAttribute(TrackSize Columns, TrackSize Rows) : RegionAttribute;    

    public record RegionJustifyColumnsAttribute(AlignmentKind Kind) : RegionAttribute;
    public record RegionJustifyRowsAttribute(AlignmentKind Kind) : RegionAttribute;
    public record RegionJustifyAttribute(AlignmentKind Columns, AlignmentKind Rows) : RegionAttribute;

    public abstract record NodeAttribute;
    public record NodeLabelAttribute(string? Content) : NodeAttribute;
    public record NodeRowAttribute(int Value) : NodeAttribute;
    public record NodeColumnAttribute(int Value) : NodeAttribute;    
    public record NodeMinWidthAttribute(int Value) : NodeAttribute;
    public record NodeMinHeightAttribute(int Value) : NodeAttribute;
    public record NodeShapeAttribute(ShapeKind? Kind) : NodeAttribute;
    public record NodeAlignColumnAttribute(AlignmentKind? Kind) : NodeAttribute;
    public record NodeAlignRowAttribute(AlignmentKind? Kind) : NodeAttribute;
    public record NodeAlignAttribute(AlignmentKind? Columns, AlignmentKind? Rows) : NodeAttribute;
    public record NodeMarginAttribute(Border Value) : NodeAttribute;
    public record NodeCornerRadiusAttribute(int Value) : NodeAttribute;

    public abstract record ArrowAttribute;
    public record ArrowAnchorStartAttribute(Anchor Anchor) : ArrowAttribute;
    public record ArrowAnchorEndAttribute(Anchor Anchor) : ArrowAttribute;
    public record ArrowAnchorAttribute(Anchor Start, Anchor End) : ArrowAttribute;
    public record ArrowOffsetStartXAttribute(int Offset) : ArrowAttribute;
    public record ArrowOffsetStartYAttribute(int Offset) : ArrowAttribute;
    public record ArrowOffsetEndXAttribute(int Offset) : ArrowAttribute;
    public record ArrowOffsetEndYAttribute(int Offset) : ArrowAttribute;
    public record ArrowOffsetXAttribute(int Start, int End) : ArrowAttribute;
    public record ArrowOffsetYAttribute(int Start, int End) : ArrowAttribute;

    [GenerateOneOf] public partial class ObjectAttribute : OneOfBase<NodeAttribute, RegionAttribute, TextAttribute, LineAttribute> { }
    [GenerateOneOf] public partial class SegmentAttribute : OneOfBase<ArrowAttribute, LineAttribute> { } 
    [GenerateOneOf] public partial class DiagramAttribute : OneOfBase<DocumentAttribute, RegionAttribute, TextAttribute> { }

    public record ClassCall(Parse.Identifier Name, Parse.Macro[] Arguments) : Parse.Templated; // XXX it would be nice if inheritance could call classes
    public record LineSegment(Parse.Identifier Target, ArrowKind? Direction);

    /*********************************************
     * Untyped AST, containing unresolved macros *
     *********************************************/
    public record UntypedAttribute(Parse.Identifier Key, Parse.Macro Value);
    public record Argument(Parse.Identifier Name, Parse.Macro? Default);
    public record ArgumentList(Argument[] Variables) : Parse.Templated;
    public record UntypedClass(Parse.Identifier Name, ArgumentList Arguments, Parse.Identifier[] BaseClasses, UntypedAttribute[] Attributes) : Parse.Templated;
    [GenerateOneOf] public partial class UntypedObjectContent : OneOfBase<ObjectAttribute, UntypedObject, UntypedLine> { }
    public record UntypedObject(ClassCall[] Classes, Parse.Identifier? Name, ObjectAttribute[] Attributes, UntypedObjectContent[] Children);
    public record UntypedLine(ClassCall[] Classes, LineSegment[] Segments, SegmentAttribute[] Attributes);
    [GenerateOneOf] public partial class UntypedDocumentContent : OneOfBase<DiagramAttribute, UntypedClass, TypedClass /* for better errors */, UntypedObject, UntypedLine> { }
    public record UntypedDocument(UntypedDocumentContent[] Declarations);

    /*************
     * Typed AST *
     *************/
    public abstract record TypedClass(Parse.Identifier Name, Parse.Identifier[] BaseClasses);
    public record ObjectClass(Parse.Identifier Name, Parse.Identifier[] BaseClasses, ObjectAttribute[] Attributes) : TypedClass(Name, BaseClasses);
    public record LineClass(Parse.Identifier Name, Parse.Identifier[] BaseClasses, SegmentAttribute[] Attributes) : TypedClass(Name, BaseClasses);
    public record ObjectOrLineClass(Parse.Identifier Name, Parse.Identifier[] BaseClasses, LineAttribute[] Attributes) : TypedClass(Name, BaseClasses);
    [GenerateOneOf] public partial class TypedObjectContent : OneOfBase<ObjectAttribute, TypedObject, TypedLine> { }
    public record TypedObject(Parse.Identifier[] Classes, Parse.Identifier? Name, ObjectAttribute[] Attributes, TypedObjectContent[] Children);
    public record TypedLine(Parse.Identifier[] Classes, LineSegment[] Segments, SegmentAttribute[] Attributes);
    [GenerateOneOf] public partial class TypedDocumentContent : OneOfBase<DiagramAttribute, TypedClass, TypedObject, TypedLine> { }
    public record TypedDocument(TypedDocumentContent[] Declarations);
}
