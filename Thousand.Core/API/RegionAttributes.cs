using Superpower;
using System.Collections.Generic;
using Thousand.Model;
using Thousand.Parse;

namespace Thousand.API
{
    // an object contains a region, and the entire diagram is also a region
    public static class RegionAttributes
    {
        private static readonly AttributeGroup<AST.RegionAttribute> Definition = new(UseKind.Region);

        public static TokenListParser<TokenKind, (AlignmentKind columns, AlignmentKind rows)> Justification { get; } =
            Value.AlignColumnOnly.Then(c => Value.AlignRow.OptionalOrDefault(AlignmentKind.Center).Select(r => (c, r)))
                .Or(Value.AlignRowOnly.Then(r => Value.AlignColumnOnly.OptionalOrDefault(AlignmentKind.Center).Select(c => (c, r))))
                .Or(Identifier.Enum<AlignmentKind>().Then(cOrBoth => Value.AlignRow.Select(r => new AlignmentKind?(r)).OptionalOrDefault(default(AlignmentKind?)).Select(rOrNeither => (cOrBoth, rOrNeither ?? cOrBoth))));

        private static AttributeType<(FlowKind?, int?)> GridShorthand { get; } = new(
            Attribute.ShorthandVV(Identifier.Enum<FlowKind>(), Value.CountingNumber.Named("grid track")),
            "any or all of: `columns` or `rows` (flow direction), `M` (max row/column number)", 
            "rows", "3 rows", "5 columns"
        );

        private static AttributeType<(AlignmentKind columns, AlignmentKind rows)> JustifyShorthand { get; } = new(
            Justification,
            "both axes (`top`, `left`, `bottom`, `right`) or a single shared value (`stretch`, `start`, `center`, `end`)",
            "start", "center", "top right", "right end"
        );

        public static IEnumerable<AttributeDefinition<AST.RegionAttribute>> All()
        {
            yield return Definition.Create("scale", AttributeType.PixelSize("S"), value => new AST.RegionScaleAttribute(value),
                "Multiplies the size of everything inside the area by S."
            );

            yield return Definition.Create("fill", AttributeType.ColourOptional, value => new AST.RegionFillAttribute(value),
                "If set, gives the area a background colour. (Otherwise, it's transparent.)"
            );

            yield return Definition.Create("padding-left", AttributeType.PixelSize("X"), value => new AST.RegionPaddingAttribute(value, null, null, null),
                "Adds `X` pixels of space inside the left edge of the area."
            );
            yield return Definition.Create("padding-top", AttributeType.PixelSize("Y"), value => new AST.RegionPaddingAttribute(null, value, null, null),
                "Adds `Y` pixels of space beneath the top edge of the area."
            );
            yield return Definition.Create("padding-right", AttributeType.PixelSize("X"), value => new AST.RegionPaddingAttribute(null, null, value, null),
                "Adds `X` pixels of space inside the right edge of the area."
            );
            yield return Definition.Create("padding-bottom", AttributeType.PixelSize("Y"), value => new AST.RegionPaddingAttribute(null, null, null, value),
                "Adds `Y` pixels of space above the bottom edge of the area."
            );
            yield return Definition.Create("padding", AttributeType.Border, value => new AST.RegionPaddingAttribute(value.Left, value.Top, value.Right, value.Bottom),
                "Adds space inside the area, making it larger than its contents."
            );

            yield return Definition.Create("grid-flow", AttributeType.Enum<FlowKind>(), value => new AST.RegionGridFlowAttribute(value),
                "Lay out the area in columns (horizontally) or rows (vertically)."
            );
            yield return Definition.Create("grid-max", AttributeType.GridTrack("M"), value => new AST.RegionGridMaxAttribute(value),
                "The area will grow to `M` columns/rows and then start a new row/column."
            );
            yield return Definition.Create("grid", GridShorthand, values => values switch { (var flow, var max) => new AST.RegionGridAttribute(flow, max) },
                "Lay out up to `M` columns/rows in the specified flow direction."
            );

            yield return Definition.Create("gutter-horizontal", AttributeType.PixelSize("X"), value => new AST.RegionGutterColumnsAttribute(value),
                "Leave a gap of `X` pixels between grid columns."
            );
            yield return Definition.Create("gutter-vertical", AttributeType.PixelSize("Y"), value => new AST.RegionGutterRowsAttribute(value),
                "Leave a gap of `Y` pixels between grid rows."
            );
            yield return Definition.Create("gutter", AttributeType.PixelSize("X/Y").Twice(), values => new AST.RegionGutterAttribute(values.first, values.second),
                "Leave a gap of `X/Y` pixels between grid tracks."
            );

            yield return Definition.Create("layout-columns", AttributeType.GridSize, value => new AST.RegionLayoutColumnsAttribute(value),
                "The width of the area's grid columns."
            );
            yield return Definition.Create("layout-rows", AttributeType.GridSize, value => new AST.RegionLayoutRowsAttribute(value),
                "The height of the area's grid rows."
            );
            yield return Definition.Create("layout", AttributeType.GridSize.Twice(), values => new AST.RegionLayoutAttribute(values.first, values.second),
                "The width and height of the area's grid tracks."
            );

            yield return Definition.Create("justify-columns", AttributeType.AlignColumn, value => new AST.RegionJustifyColumnsAttribute(value),
                "Positions objects horizontally within the area's grid columns."
            );
            yield return Definition.Create("justify-rows", AttributeType.AlignRow, value => new AST.RegionJustifyRowsAttribute(value),
                "Positions objects vertically within the area's grid rows."
            );
            yield return Definition.Create("justify", JustifyShorthand, values => new AST.RegionJustifyAttribute(values.columns, values.rows),
                "Positions objects horizontally and vertically within the area's grid tracks."
            );
        }
    }
}
