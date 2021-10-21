using OneOf;
using System;
using System.Linq;
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
    public record RegionGridFlowAttribute(FlowKind Kind) : RegionAttribute;
    public record RegionGridMaxAttribute(int Value) : RegionAttribute;
    public record RegionGridAttribute(FlowKind? Flow, int? Max) : RegionAttribute;

    public record RegionSpaceColumnsAttribute(int Value) : RegionAttribute;
    public record RegionSpaceRowsAttribute(int Value) : RegionAttribute;
    public record RegionSpaceAttribute(int Columns, int Rows) : RegionAttribute;

    public record RegionLayoutColumnsAttribute(TrackSize Size) : RegionAttribute;
    public record RegionLayoutRowsAttribute(TrackSize Size) : RegionAttribute;
    public record RegionLayoutAttribute(TrackSize Columns, TrackSize Rows) : RegionAttribute;

    public record RegionJustifyColumnsAttribute(AlignmentKind Kind) : RegionAttribute;
    public record RegionJustifyRowsAttribute(AlignmentKind Kind) : RegionAttribute;
    public record RegionJustifyAttribute(AlignmentKind Columns, AlignmentKind Rows) : RegionAttribute;

    public abstract record PositionAttribute;
    public record PositionAnchorAttribute(CompassKind Placement) : PositionAttribute;
    public record PositionOffsetAttribute(Point Offset) : PositionAttribute;

    public abstract record NodeAttribute;
    public record NodeLabelContentAttribute(Text Text) : NodeAttribute;
    public record NodeLabelJustifyAttribute(AlignmentKind Kind) : NodeAttribute;
    public record NodeLabelAttribute(Text? Content, AlignmentKind? Justify) : NodeAttribute;
    public record NodeColumnAttribute(int Value) : NodeAttribute;
    public record NodeRowAttribute(int Value) : NodeAttribute;
    public record NodePositionAttribute(Point Origin) : NodeAttribute;
    public record NodeMinWidthAttribute(decimal? Value) : NodeAttribute;
    public record NodeMinHeightAttribute(decimal? Value) : NodeAttribute;
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
    public record ArrowOffsetStartAttribute(Point Offset) : ArrowAttribute;
    public record ArrowOffsetEndAttribute(Point Offset) : ArrowAttribute;
    public record ArrowOffsetAttribute(Point Start, Point End) : ArrowAttribute;

    [GenerateOneOf] public partial class EntityAttribute : OneOfBase<PositionAttribute, LineAttribute> { }
    [GenerateOneOf] public partial class ObjectAttribute : OneOfBase<PositionAttribute, NodeAttribute, RegionAttribute, TextAttribute, LineAttribute> { }
    [GenerateOneOf] public partial class SegmentAttribute : OneOfBase<PositionAttribute, ArrowAttribute, LineAttribute> { }
    [GenerateOneOf] public partial class DiagramAttribute : OneOfBase<DocumentAttribute, RegionAttribute, TextAttribute> { }

    public record ClassCall(Parse.Identifier Name, Parse.Macro[] Arguments); // XXX it would be nice if inheritance could call classes
    public record LineSegment(Parse.Identifier Target, ArrowKind? Direction)
    {
        public LineSegment(string target, ArrowKind? direction) : this(new Parse.Identifier(target), direction) { }
    }

    /*****************************************************************************
     * Error-tolerant AST, containing invalid declarations and unresolved macros *
     *****************************************************************************/
    public record InvalidDeclaration;
    public record UntypedAttribute(Parse.Identifier Key, Parse.Macro Value);
    public record Argument(Parse.Identifier Name, Parse.Macro? Default);   

    public record UntypedClass(Parse.Identifier Name, Parse.Macro<Argument[]> Arguments, Parse.Macro<ClassCall>[] BaseClasses, UntypedAttribute[] Attributes, Parse.Macro<UntypedObjectContent>[] Declarations);

    [GenerateOneOf] public partial class UntypedObjectContent : OneOfBase<InvalidDeclaration, UntypedAttribute, UntypedClass, UntypedObject, UntypedLine> { }
    public record UntypedObject(Parse.Macro<ClassCall>[] Classes, Parse.Identifier? Name, UntypedAttribute[] Attributes, Parse.Macro<UntypedObjectContent>[] Declarations);

    public record UntypedLine(Parse.Macro<ClassCall>[] Classes, LineSegment[] Segments, UntypedAttribute[] Attributes);

    [GenerateOneOf] public partial class UntypedDocumentContent : OneOfBase<InvalidDeclaration, UntypedAttribute, UntypedClass, UntypedObject, UntypedLine> { }
    public record UntypedDocument(Parse.Macro<UntypedDocumentContent>[] Declarations);

    /****************************************************************
     * Strict AST, with macros resolved and attributes fully parsed *
     ****************************************************************/
    public abstract record TypedClass(Parse.Identifier Name, Parse.Identifier[] BaseClasses);
    public record ObjectClass(Parse.Identifier Name, Parse.Identifier[] BaseClasses, ObjectAttribute[] Attributes, TypedObjectContent[] Declarations) : TypedClass(Name, BaseClasses)
    {
        public ObjectClass(string name, params ObjectAttribute[] attrs) : this(new Parse.Identifier(name), Array.Empty<Parse.Identifier>(), attrs, Array.Empty<TypedObjectContent>()) { }
    }
    public record LineClass(Parse.Identifier Name, Parse.Identifier[] BaseClasses, SegmentAttribute[] Attributes) : TypedClass(Name, BaseClasses)
    {
        public LineClass(string name, params SegmentAttribute[] attrs) : this(new Parse.Identifier(name), Array.Empty<Parse.Identifier>(), attrs) { }
    }
    public record ObjectOrLineClass(Parse.Identifier Name, Parse.Identifier[] BaseClasses, EntityAttribute[] Attributes) : TypedClass(Name, BaseClasses);

    [GenerateOneOf] public partial class TypedObjectContent : OneOfBase<ObjectAttribute, TypedClass, TypedObject, TypedLine> { }
    public record TypedObject(Parse.Identifier[] Classes, Parse.Identifier? Name, ObjectAttribute[] Attributes, TypedObjectContent[] Declarations)
    {
        public TypedObject(string klass, string? name, ObjectAttribute[] attrs, params TypedObjectContent[] content) : this(new Parse.Identifier[] { new(klass) }, name == null ? null : new Parse.Identifier(name), attrs, content) { }
        public TypedObject(string klass, string? name, params ObjectAttribute[] attrs) : this(new Parse.Identifier[] { new(klass) }, name == null ? null : new Parse.Identifier(name), attrs, Array.Empty<TypedObjectContent>()) { }
    }

    public record TypedLine(Parse.Identifier[] Classes, LineSegment[] Segments, SegmentAttribute[] Attributes)
    {
        public TypedLine(string klass, params LineSegment[] segs) : this(new Parse.Identifier[] { new(klass) }, segs, Array.Empty<SegmentAttribute>()) { }
    }

    [GenerateOneOf] public partial class TypedDocumentContent : OneOfBase<DiagramAttribute, TypedClass, TypedObject, TypedLine> { }
    public record TypedDocument(TypedDocumentContent[] Declarations);
}
