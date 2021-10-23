using System.Collections.Generic;
using Definition = Thousand.Parse.Attributes.AttributeDefinition<Thousand.AST.DiagramAttribute>;

namespace Thousand.Parse.Attributes
{
    // diagram group, used only by documents
    static class DiagramAttributes
    {
        public static IEnumerable<Definition> All()
        {
            yield return Definition.Create("scale", Value.CountingDecimal, value => new AST.DiagramScaleAttribute(value));
        }
    }
}
