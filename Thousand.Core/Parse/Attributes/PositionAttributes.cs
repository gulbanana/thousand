using System.Collections.Generic;
using Definition = Thousand.Parse.Attributes.AttributeDefinition<Thousand.AST.PositionAttribute>;

namespace Thousand.Parse.Attributes
{
    // entity position group, used by objects and lines
    static class PositionAttributes
    {
        public static IEnumerable<Definition> All()
        {
            yield return Definition.Create("anchor", Value.Anchor.Twice(), startAndEnd => new AST.PositionAnchorAttribute(startAndEnd.first, startAndEnd.second));

            yield return Definition.Create("offset", Value.Point.Twice(), startAndEnd => new AST.PositionOffsetAttribute(startAndEnd.first, startAndEnd.second));
        }
    }
}
