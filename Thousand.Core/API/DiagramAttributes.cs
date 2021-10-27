using System.Collections.Generic;
using Thousand.Parse;
using Definition = Thousand.API.AttributeDefinition<Thousand.AST.DiagramAttribute>;

namespace Thousand.API
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
