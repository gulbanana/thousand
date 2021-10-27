using Superpower;
using System.Collections.Generic;
using Thousand.Model;
using Thousand.Parse;
using Definition = Thousand.API.AttributeDefinition<Thousand.AST.RegionAttribute>;

namespace Thousand.API
{
    // region group, used by objects and documents
    public static class RegionAttributes
    {
        public static TokenListParser<TokenKind, (AlignmentKind columns, AlignmentKind rows)> Justification { get; } =
            Attribute.AlignColumnOnly.Then(c => Attribute.AlignRow.OptionalOrDefault(AlignmentKind.Center).Select(r => (c, r)))
                .Or(Attribute.AlignRowOnly.Then(r => Attribute.AlignColumn.OptionalOrDefault(AlignmentKind.Center).Select(c => (c, r))))
                .Or(Identifier.Enum<AlignmentKind>().Then(cOrBoth => Attribute.AlignRow.Select(r => new AlignmentKind?(r)).OptionalOrDefault(default(AlignmentKind?)).Select(rOrNeither => (cOrBoth, rOrNeither ?? cOrBoth))));

        public static IEnumerable<Definition> All()
        {
            yield return Definition.Create("fill", Value.Colour.OrNull(), value => new AST.RegionFillAttribute(value));

            yield return Definition.Create("padding", Value.Border, value => new AST.RegionPaddingAttribute(value));

            yield return Definition.Create("grid-flow", Identifier.Enum<FlowKind>(), value => new AST.RegionGridFlowAttribute(value));
            yield return Definition.Create("grid-max", Value.CountingNumber, value => new AST.RegionGridMaxAttribute(value));
            yield return Definition.Create("grid", Attribute.ShorthandVV(Identifier.Enum<FlowKind>(), Value.CountingNumber), 
                                                   values => values switch { (var flow, var max) => new AST.RegionGridAttribute(flow, max) });

            yield return Definition.Create("space-columns", "gutter-columns", Value.WholeNumber, value => new AST.RegionSpaceColumnsAttribute(value));
            yield return Definition.Create("space-rows", "gutter-rows", Value.WholeNumber, value => new AST.RegionSpaceRowsAttribute(value));
            yield return Definition.Create("space", "gutter", Value.WholeNumber.Twice(), values => new AST.RegionSpaceAttribute(values.first, values.second));

            yield return Definition.Create("layout-columns", Value.TrackSize, value => new AST.RegionLayoutColumnsAttribute(value));
            yield return Definition.Create("layout-rows", Value.TrackSize, value => new AST.RegionLayoutRowsAttribute(value));
            yield return Definition.Create("layout", Value.TrackSize.Twice(), values => new AST.RegionLayoutAttribute(values.first, values.second));

            yield return Definition.Create("justify-columns", Attribute.AlignColumn, value => new AST.RegionJustifyColumnsAttribute(value));
            yield return Definition.Create("justify-rows", Attribute.AlignRow, value => new AST.RegionJustifyRowsAttribute(value));
            yield return Definition.Create("justify", Justification, values => new AST.RegionJustifyAttribute(values.columns, values.rows));
        }
    }
}
