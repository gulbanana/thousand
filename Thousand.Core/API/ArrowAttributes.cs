using System.Collections.Generic;

namespace Thousand.API
{
    static class ArrowAttributes
    {
        private static readonly AttributeGroup<AST.ArrowAttribute> Definition = new(UseKind.Line);

        public static IEnumerable<AttributeDefinition<AST.ArrowAttribute>> All()
        {
            yield return Definition.Create("anchor-start", AttributeType.Anchor, anchor => new AST.ArrowAnchorStartAttribute(anchor),
                "Defines the connection behaviour of the line's start point."
            );
            yield return Definition.Create("anchor-end", AttributeType.Anchor, anchor => new AST.ArrowAnchorEndAttribute(anchor),
                "Defines the connection behaviour of the line's end point."
            );

            yield return Definition.Create("offset-start", AttributeType.PointRelative, point => new AST.ArrowOffsetStartAttribute(point),
                "Adjusts the position of the of the line's start point."
            );
            yield return Definition.Create("offset-end", AttributeType.PointRelative, point => new AST.ArrowOffsetEndAttribute(point),
                "Adjusts the position of the of the line's end point."
            );
        }
    }
}
