using Superpower;
using System.Collections.Generic;
using Thousand.Model;
using Definition = Thousand.Parse.Attributes.AttributeDefinition<Thousand.AST.NodeAttribute>;

namespace Thousand.Parse.Attributes
{
    // node group, used only by objects
    // a node is something that can be positioned within a region. it may have a drawn shape or intrinsic text content
    public static class NodeAttributes
    {
        public static TokenListParser<TokenKind, (AlignmentKind? horizontal, AlignmentKind? vertical)> Alignment { get; } =
            Attribute.AlignColumnOnly.Then(c => Attribute.AlignRow.OrNone().OptionalOrDefault(default(AlignmentKind?)).Select(r => (new AlignmentKind?(c), r)))
                .Or(Attribute.AlignRowOnly.Then(r => Attribute.AlignColumn.OrNone().OptionalOrDefault(default(AlignmentKind?)).Select(c => (c, new AlignmentKind?(r)))))
                .Or(Identifier.Enum<AlignmentKind>().OrNone().Then(cOrBoth => Attribute.AlignRow.OrNone().OptionalOrDefault(default(AlignmentKind?)).Select(rOrNeither => (cOrBoth, rOrNeither ?? cOrBoth))));

        public static IEnumerable<Definition> All()
        {
            // controls for the intrinsic content 
            yield return Definition.Create("min-width", Value.CountingDecimal.OrNone(), value => new AST.NodeMinWidthAttribute(value));
            yield return Definition.Create("min-height", Value.CountingDecimal.OrNone(), value => new AST.NodeMinHeightAttribute(value));

            yield return Definition.Create("shape", Identifier.Enum<ShapeKind>().OrNone(), value => new AST.NodeShapeAttribute(value));
            yield return Definition.Create("corner-radius", "corner", Value.WholeNumber, value => new AST.NodeCornerRadiusAttribute(value)); // XXX maybe this should be shape-radius or something

            yield return Definition.Create("label-content", Value.Text, value => new AST.NodeLabelContentAttribute(value));
            yield return Definition.Create("label-justify", Attribute.AlignColumn, value => new AST.NodeLabelJustifyAttribute(value));
            yield return Definition.Create("label", Attribute.Shorthand(Identifier.Enum<AlignmentKind>(), Value.Text), 
                                                    values => values switch { (var a, var t) => new AST.NodeLabelAttribute(t, a) });

            // different methods of placing the node in its region - see also "anchor" from EntityAttributes
            yield return Definition.Create("column", "col", Value.CountingNumber, value => new AST.NodeColumnAttribute(value));
            yield return Definition.Create("row", Value.CountingNumber, value => new AST.NodeRowAttribute(value));       
            
            yield return Definition.Create("position", "pos", from x in Value.WholeNumber from y in Value.WholeNumber select new Point(x, y), value => new AST.NodePositionAttribute(value));

            // modifies the tracks in which (or the anchors on which) the node is placed
            yield return Definition.Create("margin", Value.Border, value => new AST.NodeMarginAttribute(value));

            // modifies placement *within* grid tracks (or beside anchors) - the counterpart of "justify" from RegionAttributes
            yield return Definition.Create("align-horizontal", Attribute.AlignColumn.OrNone(), value => new AST.NodeAlignColumnAttribute(value));
            yield return Definition.Create("align-vertical", Attribute.AlignRow.OrNone(), value => new AST.NodeAlignRowAttribute(value));
            yield return Definition.Create("align", Alignment, values => new AST.NodeAlignAttribute(values.horizontal, values.vertical));
        }
    }
}
