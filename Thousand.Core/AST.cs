using OneOf;
using Superpower.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Thousand.Model;

// Intermediate representation shared between Parse and Canonicalise stages
namespace Thousand.AST
{
    /***********************************************************************************
     * Attributes - produced from UntypedAttribute as part of the typechecking process *
     ***********************************************************************************/
    public abstract record TextAttribute;
    public record TextFontFamilyAttribute(string Name) : TextAttribute;
    public record TextFontSizeAttribute(int Value) : TextAttribute;
    public record TextFontColourAttribute(Colour Colour) : TextAttribute;
    public record TextFontAttribute(string? Family, int? Size, Colour? Colour) : TextAttribute;

    public abstract record EntityAttribute
    {
        public virtual bool IsLineOnly() => false;
    }

    public record EntityAnchorAttribute(Anchor Start, Anchor End) : EntityAttribute
    {
        public override bool IsLineOnly() => Start is not SpecificAnchor || !Start.Equals(End);
    }

    public record EntityOffsetAttribute(Point Start, Point End) : EntityAttribute
    {
        public override bool IsLineOnly() => !Start.Equals(End);
    }

    public record EntityStrokeColourAttribute(Colour Colour) : EntityAttribute;
    public record EntityStrokeStyleAttribute(StrokeKind Kind) : EntityAttribute;
    public record EntityStrokeWidthAttribute(Width Value) : EntityAttribute;
    public record EntityStrokeAttribute(Colour? Colour, StrokeKind? Style, Width? Width) : EntityAttribute;

    public record EntityLabelContentAttribute(string Content) : EntityAttribute;
    public record EntityLabelJustifyAttribute(AlignmentKind Kind) : EntityAttribute;
    public record EntityLabelOffsetAttribute(Point Offset) : EntityAttribute;
    public record EntityLabelAttribute(Point? Offset, Text? Content, AlignmentKind? Justify) : EntityAttribute;

    public abstract record RegionAttribute;
    public record RegionScaleAttribute(decimal Value) : RegionAttribute;
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

    public abstract record NodeAttribute;
    public record NodeColumnAttribute(int Value) : NodeAttribute;
    public record NodeRowAttribute(int Value) : NodeAttribute;
    public record NodePositionAttribute(Point Origin) : NodeAttribute;
    public record NodeMinWidthAttribute(decimal? Value) : NodeAttribute;
    public record NodeMinHeightAttribute(decimal? Value) : NodeAttribute;
    public record NodeShapeAttribute(ShapeKind? Kind) : NodeAttribute;
    public record NodeCornerRadiusAttribute(int Value) : NodeAttribute;
    public record NodeMarginAttribute(Border Value) : NodeAttribute;

    public record NodeAlignColumnAttribute(AlignmentKind? Kind) : NodeAttribute;
    public record NodeAlignRowAttribute(AlignmentKind? Kind) : NodeAttribute;
    public record NodeAlignAttribute(AlignmentKind? Columns, AlignmentKind? Rows) : NodeAttribute;

    public abstract record ArrowAttribute;
    public record ArrowAnchorStartAttribute(Anchor Anchor) : ArrowAttribute;
    public record ArrowAnchorEndAttribute(Anchor Anchor) : ArrowAttribute;
    public record ArrowOffsetStartAttribute(Point Offset) : ArrowAttribute;
    public record ArrowOffsetEndAttribute(Point Offset) : ArrowAttribute;

    [GenerateOneOf] public partial class DocumentAttribute : OneOfBase<RegionAttribute, TextAttribute> { }
    [GenerateOneOf] public partial class ObjectAttribute : OneOfBase<EntityAttribute, NodeAttribute, RegionAttribute, TextAttribute> { }
    [GenerateOneOf] public partial class LineAttribute : OneOfBase<EntityAttribute, ArrowAttribute, TextAttribute> { }

    /********************
     * Shared AST parts *
     ********************/

    public record LineSegment<T>(OneOf<Name, T> Target, ArrowKind? Direction);

    /*****************************************************************************
     * Error-tolerant AST, containing invalid declarations and unresolved macros *
     *****************************************************************************/

    public abstract record UntypedDeclaration;
    public record InvalidDeclaration : UntypedDeclaration;
    public record EmptyDeclaration : UntypedDeclaration;

    public struct UntypedAttribute
    {
        public Name? Key { get; }
        public bool HasEqualsSign { get; }
        public Parse.IMacro Value { get; }

        public UntypedAttribute(Name? key, bool hasEqualsSign, Parse.IMacro value)
        {
            Key = key;
            HasEqualsSign = hasEqualsSign;
            Value = value;
        }

        public bool HasValue => Value.Location.Position < Value.Remainder.Position;
    }

    public record Attributes(Parse.IMacro<bool> IsComplete, IReadOnlyList<UntypedAttribute> Values) : IReadOnlyList<UntypedAttribute>
    {
        public UntypedAttribute this[int index] => Values[index];
        public int Count => Values.Count;
        public IEnumerator<UntypedAttribute> GetEnumerator() => Values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => Values.GetEnumerator();
    }

    public record Argument(Name Name, Parse.IMacro? Default);
    public record ClassCall(Name Name, Parse.IMacro[] Arguments); // XXX it would be nice if inheritance could call classes
    public record UntypedClass(Name Name, Parse.IMacro<Argument[]> Arguments, Parse.IMacro<ClassCall?>[] BaseClasses, Attributes Attributes, IReadOnlyList<Parse.IMacro<UntypedDeclaration>> Declarations) : UntypedDeclaration;

    public record UntypedObject(Parse.IMacro<ClassCall?>[] Classes, Name? Name, Attributes Attributes, IReadOnlyList<Parse.IMacro<UntypedDeclaration>> Declarations) : UntypedDeclaration
    {
        private readonly Lazy<string> typeName = new(() =>
        {
            return string.Join('.', Classes.Select(c => c.SpanOrEmpty().ToStringValue()));
        });

        private readonly Lazy<TextSpan> typeSpan = new(() =>
        {
            var first = Classes[0].Location.First().Span;
            var last = Classes.Last().Location.First().Span;
            return new(first.Source!, first.Position, last.Position.Absolute - first.Position.Absolute + last.Length);
        });

        public string TypeName => typeName.Value;
        public TextSpan TypeSpan => typeSpan.Value;
    }

    public record UntypedInline(Parse.IMacro<bool> IsComplete, Parse.IMacro<UntypedObject> Declaration);
    public record UntypedLine(Parse.IMacro<ClassCall?>[] Classes, LineSegment<UntypedInline>[] Segments, Attributes Attributes) : UntypedDeclaration;

    public record UntypedDocument(IReadOnlyList<Parse.IMacro<UntypedDeclaration>> Declarations);

    /****************************************************************
     * Strict AST, with macros resolved and attributes fully parsed *
     ****************************************************************/

    public abstract record TypedDeclaration;

    public abstract record TypedClass(Name Name, Name[] BaseClasses) : TypedDeclaration;
    public record ObjectClass(Name Name, Name[] BaseClasses, ObjectAttribute[] Attributes, IReadOnlyList<TypedDeclaration> Declarations) : TypedClass(Name, BaseClasses);
    public record LineClass(Name Name, Name[] BaseClasses, LineAttribute[] Attributes) : TypedClass(Name, BaseClasses);
    public record ObjectOrLineClass(Name Name, Name[] BaseClasses, EntityAttribute[] Attributes) : TypedClass(Name, BaseClasses);

    public record TypedObject(Name[] Classes, Name? Name, ObjectAttribute[] Attributes, IReadOnlyList<TypedDeclaration> Declarations) : TypedDeclaration;

    public record TypedLine(Name[] Classes, LineSegment<TypedObject>[] Segments, LineAttribute[] Attributes) : TypedDeclaration;

    public record TypedDocument(IReadOnlyList<TypedDeclaration> Declarations);
}
