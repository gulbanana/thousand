using Superpower;
using System.Collections.Generic;
using System.Linq;
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
            yield return Definition.Create("min-width", Value.CountingDecimal.OrNone(), value => new AST.NodeMinWidthAttribute(value), Format.Doc(
                "Increases an object's width to at least `X` pixels. If its content is wider than `X-padding`, it will grow larger.",
                "`X` (decimal) or `none`",
                UseKind.Object,
                "min-width=100", 
                "min-width=none"
            ));

            yield return Definition.Create("min-height", Value.CountingDecimal.OrNone(), value => new AST.NodeMinHeightAttribute(value), Format.Doc(
                "Increases an object's height to at least `Y` pixels. If its content is taller than `X-padding`, it will grow larger.",
                "`Y` (decimal) or `none`",
                UseKind.Object,
                "min-height=100",
                "min-height=none"
            ));

            yield return Definition.Create("shape", Identifier.Enum<ShapeKind>().OrNone(), value => new AST.NodeShapeAttribute(value), Format.Doc(
                Format.NamesOrNone<ShapeKind>() + " (in which case the object is just a container)",
                UseKind.Object,
                "shape=pill",
                "shape=none"
            ));

            yield return Definition.Create("corner-radius", "corner", Value.WholeNumber, value => new AST.NodeCornerRadiusAttribute(value), Format.Doc(
                "For an object with rounded corners, sets the width and height of the corners to `X`.",
                "`X` (integer)",
                UseKind.Object,
                "corner-radius=5",
                "corner=0"
            ));

            // different methods of placing the node in its region - see also "anchor" from EntityAttributes
            yield return Definition.Create("column", "col", Value.CountingNumber, value => new AST.NodeColumnAttribute(value), Format.Doc(
                "`X` (integer)",
                UseKind.Object
            ));

            yield return Definition.Create("row", Value.CountingNumber, value => new AST.NodeRowAttribute(value), Format.Doc(
                "`X` (integer)",
                UseKind.Object
            ));       
            
            yield return Definition.Create("position", "pos", from x in Value.WholeNumber from y in Value.WholeNumber select new Point(x, y), value => new AST.NodePositionAttribute(value), Format.Doc(
                "`X Y` (point)",
                UseKind.Object,
                "pos=100 0",
                "position=20 20"
            ));

            // modifies the tracks in which (or the anchors on which) the node is placed
            yield return Definition.Create("margin", Value.Border, value => new AST.NodeMarginAttribute(value), Format.Doc(
                "`XY`/`X Y`/`X1 Y1 X2 Y2` (border)",
                UseKind.Object
            ));

            // modifies placement *within* grid tracks (or beside anchors) - the counterpart of "justify" from RegionAttributes
            yield return Definition.Create("align-horizontal", Attribute.AlignColumn.OrNone(), value => new AST.NodeAlignColumnAttribute(value), Format.Doc(
                "`stretch`, `start`/`left`, `center`, `end`/`right` or `none`",
                UseKind.Object
            ));

            yield return Definition.Create("align-vertical", Attribute.AlignRow.OrNone(), value => new AST.NodeAlignRowAttribute(value), Format.Doc(
                "`stretch`, `start`/`top`, `center`, `end`/`bottom` or `none`",
                UseKind.Object
            ));

            yield return Definition.Create("align", Alignment, values => new AST.NodeAlignAttribute(values.horizontal, values.vertical), Format.Doc(
                "Shorthand for `align-horizontal` and `align-vertical alignment`.",
                "both values or a single shared value (`stretch`, `start`, `center`, `end` or `none`)",
                UseKind.Object,
                "align=center",
                "align=top right",
                "align=none end"
            ));
        }
    }
}
