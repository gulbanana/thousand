using System.Collections.Generic;
using Superpower;

namespace Thousand.Parse.Attributes
{
    public enum PositionAttributeKind
    {
        Anchor,        
        Offset,
    }

    static class PositionAttributes
    {
        public static IEnumerable<AttributeDefinition<AST.PositionAttribute>> All()
        {
            yield return new("anchor", from startAndEnd in Value.Anchor.Twice()
                                       select new AST.PositionAnchorAttribute(startAndEnd.first, startAndEnd.second) as AST.PositionAttribute);

            yield return new("offset", from startAndEnd in Value.Point.Twice()
                                       select new AST.PositionOffsetAttribute(startAndEnd.first, startAndEnd.second) as AST.PositionAttribute);
        }
    }
}
