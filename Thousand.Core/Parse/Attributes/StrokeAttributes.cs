using System.Collections.Generic;
using Thousand.Model;
using Definition = Thousand.Parse.Attributes.AttributeDefinition<Thousand.AST.StrokeAttribute>;

namespace Thousand.Parse.Attributes
{
    // edge-stroke group, used by objects and lines
    static class StrokeAttributes
    {
        public static IEnumerable<Definition> All()
        {
            yield return Definition.Create("stroke-colour", "stroke-color", Value.Colour, value => new AST.StrokeColourAttribute(value));
            yield return Definition.Create("stroke-width", Value.Width, value => new AST.StrokeWidthAttribute(value));
            yield return Definition.Create("stroke-style", Identifier.Enum<StrokeKind>(), value => new AST.StrokeStyleAttribute(value));
            yield return Definition.Create("stroke", Attribute.Shorthand(Value.Colour, Value.Width, Identifier.Enum<StrokeKind>()), 
                                                     values => values switch { (var c, var w, var s) => new AST.StrokeShorthandAttribute(c, s, w) });
        }
    }
}
