using Superpower;
using System.Collections.Generic;

namespace Thousand.Parse.Attributes
{
    static class ArrowAttributes
    {
        public static IEnumerable<AttributeDefinition<AST.ArrowAttribute>> All()
        {
            yield return new("anchor-start", from anchor in Value.Anchor
                                             select new AST.ArrowAnchorStartAttribute(anchor) as AST.ArrowAttribute);

            yield return new("anchor-end", from anchor in Value.Anchor
                                           select new AST.ArrowAnchorEndAttribute(anchor) as AST.ArrowAttribute);

            yield return new("offset-start", from point in Value.Point
                                             select new AST.ArrowOffsetStartAttribute(point) as AST.ArrowAttribute);

            yield return new("offset-end", from anchor in Value.Point
                                           select new AST.ArrowOffsetEndAttribute(anchor) as AST.ArrowAttribute);
        }
    }
}
