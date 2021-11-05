using Superpower;
using System.Collections.Generic;
using Thousand.Model;
using Thousand.Parse;
using _Definition = Thousand.API.AttributeDefinition<Thousand.AST.RegionAttribute>;

namespace Thousand.API
{
    // region group, used by objects and documents
    public static class RegionAttributes
    {
        private static readonly AttributeGroup<AST.RegionAttribute> Definition = new(UseKind.Region);

        public static TokenListParser<TokenKind, (AlignmentKind columns, AlignmentKind rows)> Justification { get; } =
            Value.AlignColumnOnly.Then(c => Value.AlignRow.OptionalOrDefault(AlignmentKind.Center).Select(r => (c, r)))
                .Or(Value.AlignRowOnly.Then(r => Value.AlignColumnOnly.OptionalOrDefault(AlignmentKind.Center).Select(c => (c, r))))
                .Or(Identifier.Enum<AlignmentKind>().Then(cOrBoth => Value.AlignRow.Select(r => new AlignmentKind?(r)).OptionalOrDefault(default(AlignmentKind?)).Select(rOrNeither => (cOrBoth, rOrNeither ?? cOrBoth))));

        private static AttributeType<(FlowKind?, int?)> GridShorthand { get; } = new(
            Attribute.ShorthandVV(Identifier.Enum<FlowKind>(), Value.CountingNumber.Named("grid track")),
            "`columns` or `rows` (flow direction) and/or `M` (max row/column number)", 
            "rows", "3 rows", "5 columns"
        );

        private static AttributeType<(AlignmentKind columns, AlignmentKind rows)> JustifyShorthand { get; } = new(
            Justification,
            "both axes (`top`, `left`, `bottom`, `right`) or a single shared value (`stretch`, `start`, `center`, `end`)",
            "start", "center", "top right", "right end"
        );

        public static IEnumerable<AttributeDefinition<AST.RegionAttribute>> All()
        {
            yield return Definition.Create("scale", AttributeType.PixelSize("S"), value => new AST.RegionScaleAttribute(value??1),
                "Multiplies the size of everything inside the area by S."
            );

            yield return Definition.Create("fill", AttributeType.ColourOptional, value => new AST.RegionFillAttribute(value),
                "If set, gives the area a background colour. (Otherwise, it's transparent.)"
            );

            yield return Definition.Create("padding", AttributeType.Border, value => new AST.RegionPaddingAttribute(value),
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

            yield return _Definition.Create("space-columns", "gutter-columns", Value.WholeNumber, value => new AST.RegionSpaceColumnsAttribute(value));
            yield return _Definition.Create("space-rows", "gutter-rows", Value.WholeNumber, value => new AST.RegionSpaceRowsAttribute(value));
            yield return _Definition.Create("space", "gutter", Value.WholeNumber.Twice(), values => new AST.RegionSpaceAttribute(values.first, values.second));

            yield return _Definition.Create("layout-columns", Value.TrackSize, value => new AST.RegionLayoutColumnsAttribute(value));
            yield return _Definition.Create("layout-rows", Value.TrackSize, value => new AST.RegionLayoutRowsAttribute(value));
            yield return _Definition.Create("layout", Value.TrackSize.Twice(), values => new AST.RegionLayoutAttribute(values.first, values.second));

            yield return Definition.Create("justify-columns", AttributeType.AlignColumn, value => new AST.RegionJustifyColumnsAttribute(value),
                "Positions objects horizontally within the area's grid tracks."
            );
            yield return Definition.Create("justify-rows", AttributeType.AlignRow, value => new AST.RegionJustifyRowsAttribute(value),
                "Positions objects vertically within the area's grid tracks."
            );
            yield return Definition.Create("justify", JustifyShorthand, values => new AST.RegionJustifyAttribute(values.columns, values.rows),
                "Positions objects within the area's grid tracks."
            );
        }
    }
}
