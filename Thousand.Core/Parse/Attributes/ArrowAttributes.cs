using System.Collections.Generic;
using Definition = Thousand.Parse.Attributes.AttributeDefinition<Thousand.AST.ArrowAttribute>;

namespace Thousand.Parse.Attributes
{
    static class ArrowAttributes
    {
        public static IEnumerable<Definition> All()
        {
            yield return Definition.Create("anchor-start", Value.Anchor, anchor => new AST.ArrowAnchorStartAttribute(anchor));
            yield return Definition.Create("anchor-end", Value.Anchor, anchor => new AST.ArrowAnchorEndAttribute(anchor));

            yield return Definition.Create("offset-start", Value.Point, point => new AST.ArrowOffsetStartAttribute(point));
            yield return Definition.Create("offset-end", Value.Point, point => new AST.ArrowOffsetEndAttribute(point));
        }
    }
}
