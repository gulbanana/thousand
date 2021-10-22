using System.Collections.Generic;
using Superpower;
using Thousand.Model;

namespace Thousand.Parse.Attributes
{
    public enum StrokeAttributeKind
    {
        Stroke,
        StrokeColour, StrokeColor = StrokeColour,
        StrokeWidth,
        StrokeStyle,
    }

    static class StrokeAttributes
    {
        public static IEnumerable<AttributeDefinition<AST.StrokeAttribute>> All()
        {
            yield return new(new[] { "stroke-colour", "stroke-color" }, from value in Value.Colour
                                                                        select new AST.StrokeColourAttribute(value) as AST.StrokeAttribute);

            yield return new("stroke-width", from value in Value.Width
                                             select new AST.StrokeWidthAttribute(value) as AST.StrokeAttribute);

            yield return new("stroke-style", from value in Identifier.Enum<StrokeKind>()
                                             select new AST.StrokeStyleAttribute(value) as AST.StrokeAttribute);

            yield return new("stroke", from values in Attribute.Shorthand(Value.Colour, Value.Width, Identifier.Enum<StrokeKind>())
                                       select values switch { (var c, var w, var s) => new AST.StrokeShorthandAttribute(c, s, w) as AST.StrokeAttribute });
        }
    }
}
