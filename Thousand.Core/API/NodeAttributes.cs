using Superpower;
using System.Collections.Generic;
using System.Linq;
using Thousand.Model;
using Thousand.Parse;

namespace Thousand.API
{
    // node group, used only by objects
    // a node is something that can be positioned within a region. it may have a drawn shape or intrinsic text content
    public static class NodeAttributes
    {
        private static readonly AttributeGroup<AST.NodeAttribute> Definition = new(UseKind.Object);

        public static TokenListParser<TokenKind, (AlignmentKind? horizontal, AlignmentKind? vertical)> Alignment { get; } =
            Value.AlignColumnOnly.Then(c => Value.AlignRow.OrNone().OptionalOrDefault(default(AlignmentKind?)).Select(r => (new AlignmentKind?(c), r)))
                .Or(Value.AlignRowOnly.Then(r => Value.AlignColumnOnly.OrNone().OptionalOrDefault(default(AlignmentKind?)).Select(c => (c, new AlignmentKind?(r)))))
                .Or(Identifier.Enum<AlignmentKind>().OrNone().Then(cOrBoth => Value.AlignRow.OrNone().OptionalOrDefault(default(AlignmentKind?)).Select(rOrNeither => (cOrBoth, rOrNeither ?? cOrBoth))));

        private static AttributeType<(AlignmentKind? horizontal, AlignmentKind? vertical)> AlignShorthand { get; } = new(
            Alignment,
            "both axes (`top`, `left`, `bottom`, `right`, `none`) or a single shared value (`stretch`, `start`, `center`, `end` or `none`)", 
            "start", "center", "top right", "left top", "none end"
        );

        public static IEnumerable<AttributeDefinition<AST.NodeAttribute>> All()
        {
            yield return Definition.Create("width", AttributeType.PixelSize("X"), value => new AST.NodeMinWidthAttribute(value),
                "Increases an object's width to a minimum of `X` pixels. (If necessary, it will grow wider.)"
            );

            yield return Definition.Create("height", AttributeType.PixelSize("Y"), value => new AST.NodeMinHeightAttribute(value),
                "Increases an object's height to a minimum of `Y` pixels. (If necessary, it will grow taller.)");

            yield return Definition.Create("shape", AttributeType.EnumOptional<ShapeKind>(), value => new AST.NodeShapeAttribute(value),
                "Selects the kind of shape to draw. (If none, the object is just an invisible container.)"
            );

            yield return Definition.Create("corner-radius", "corner", AttributeType.PixelSize("R"), value => new AST.NodeCornerRadiusAttribute(value??0),
                "For an object with rounded corners, sets the width and height of the corners to `R`."
            );

            yield return Definition.Create("column", "col", AttributeType.GridTrack("X"), value => new AST.NodeColumnAttribute(value),
                "Skips to column `X` in the containing region's grid, and places the object there."
            );

            yield return Definition.Create("row", AttributeType.GridTrack("Y"), value => new AST.NodeRowAttribute(value),
                "Skips to row `Y` in the containing region's grid, and places the object there."
            );       
            
            yield return Definition.Create("position", "pos", AttributeType.PointAbsolute, value => new AST.NodePositionAttribute(value),
                "Places the object at a fixed point within the containing region."
            );

            yield return Definition.Create("margin", AttributeType.Border, value => new AST.NodeMarginAttribute(value), 
                "Adds space around the object, increasing the size of its containing region or grid track."
            );

            yield return Definition.Create("align-horizontal", AttributeType.AlignColumnOptional, value => new AST.NodeAlignColumnAttribute(value),
                "Places the object horizontally within its grid track or on its containing anchor."
            );

            yield return Definition.Create("align-vertical", AttributeType.AlignRowOptional, value => new AST.NodeAlignRowAttribute(value),
                "Places the object vertically within its grid track or on its containing anchor."
            );

            yield return Definition.Create("align", AlignShorthand, values => new AST.NodeAlignAttribute(values.horizontal, values.vertical), 
                "Shorthand for `align-horizontal` and `align-vertical alignment`."
            );
        }
    }
}
