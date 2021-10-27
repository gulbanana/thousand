using Superpower;
using System.Collections.Generic;
using System.Linq;
using Thousand.Model;
using Thousand.Parse;
using Definition = Thousand.API.AttributeDefinition<Thousand.AST.NodeAttribute>;

namespace Thousand.API
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
            yield return Definition.Create("min-width", Value.CountingDecimal.OrNone(), value => new AST.NodeMinWidthAttribute(value),
                "Increases an object's width to at least `X` pixels. If its content is wider than `X - padding`, it will grow larger.",
                "`X` (decimal) or `none`",
                UseKind.Object,
                "min-width=100", 
                "min-width=none"
            );

            yield return Definition.Create("min-height", Value.CountingDecimal.OrNone(), value => new AST.NodeMinHeightAttribute(value),
                "Increases an object's height to at least `Y` pixels. If its content is taller than `X - padding`, it will grow larger.",
                "`Y` (decimal) or `none`",
                UseKind.Object,
                "min-height=100",
                "min-height=none"
            );

            yield return Definition.Create("shape", Identifier.Enum<ShapeKind>().OrNone(), value => new AST.NodeShapeAttribute(value),
                "Selects the kind of shape to draw (if none, the object is just an invisible container).",
                Format.NamesOrNone<ShapeKind>(),
                UseKind.Object,
                "shape=pill",
                "shape=none"
            );

            yield return Definition.Create("corner-radius", "corner", Value.WholeNumber, value => new AST.NodeCornerRadiusAttribute(value),
                "For an object with rounded corners, sets the width and height of the corners to `X`.",
                "`X` (integer)",
                UseKind.Object,
                "corner-radius=5",
                "corner=0"
            );

            yield return Definition.Create("column", "col", AttributeType.Column, value => new AST.NodeColumnAttribute(value),
                "Skips to column `X` in the containing region's grid, and places the object there.",
                UseKind.Object
            );

            yield return Definition.Create("row", AttributeType.Row, value => new AST.NodeRowAttribute(value),
                "Skips to row `Y` in the containing region's grid, and places the object there.",
                UseKind.Object
            );       
            
            yield return Definition.Create("position", "pos", AttributeType.AbsolutePoint, value => new AST.NodePositionAttribute(value),
                "Places the object at a fixed point within the containing region.",
                UseKind.Object
            );

            yield return Definition.Create("margin", Value.Border, value => new AST.NodeMarginAttribute(value), 
                "Adds space around the object, increasing the size of its containing region or grid track.",
                "`XY`/`X Y`/`X1 Y1 X2 Y2` (border)",
                UseKind.Object
            );

            yield return Definition.Create("align-horizontal", Attribute.AlignColumn.OrNone(), value => new AST.NodeAlignColumnAttribute(value),
                "Places the object horizontally within its grid track or on its containing anchor.",
                "`stretch`, `start`/`left`, `center`, `end`/`right` or `none`",
                UseKind.Object
            );

            yield return Definition.Create("align-vertical", Attribute.AlignRow.OrNone(), value => new AST.NodeAlignRowAttribute(value),
                "Places the object vertically within its grid track or on its containing anchor.",
                "`stretch`, `start`/`top`, `center`, `end`/`bottom` or `none`",
                UseKind.Object
            );

            yield return Definition.Create("align", Alignment, values => new AST.NodeAlignAttribute(values.horizontal, values.vertical), 
                "Shorthand for `align-horizontal` and `align-vertical alignment`.",
                "both values or a single shared value (`stretch`, `start`, `center`, `end` or `none`)",
                UseKind.Object,
                "align=center",
                "align=top right",
                "align=none end"
            );
        }
    }
}
