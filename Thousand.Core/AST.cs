﻿using OneOf;
using Thousand.Model;

// Intermediate representation shared between Parse and Canonicalise stages
namespace Thousand.AST
{
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
    public record RegionPaddingAttribute(int Value) : RegionAttribute;
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
    public record NodeAlignAttribute(AlignmentKind? Kind) : NodeAttribute;
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

    [GenerateOneOf] public partial class ObjectAttribute : OneOfBase<NodeAttribute, RegionAttribute, TextAttribute, LineAttribute> { }
    [GenerateOneOf] public partial class ObjectDeclaration : OneOfBase<ObjectAttribute, TypedObject, TypedLine> { }

    [GenerateOneOf] public partial class SegmentAttribute : OneOfBase<ArrowAttribute, LineAttribute> { }
    
    [GenerateOneOf] public partial class DiagramAttribute : OneOfBase<DocumentAttribute, RegionAttribute, TextAttribute> { }    
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
