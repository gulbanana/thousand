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
            yield return Definition.Create("stroke-colour", "stroke-color", Value.Colour, value => new AST.StrokeColourAttribute(value), Format.Doc(
                "Controls the colour of lines. Supports HTML-style colours or a few well-known names.",
                "`#rrggbb` or `#rgb` (colour)",
                UseKind.Entity
            ));

            yield return Definition.Create("stroke-width", Value.Width, value => new AST.StrokeWidthAttribute(value), Format.Doc(
                "Controls the presence and width of lines. Hairlines are always 1 pixel wide; lines with a set width scale with the document.",
                "`X` (number), `hairline` or `none`",
                UseKind.Entity
            ));

            yield return Definition.Create("stroke-style", Identifier.Enum<StrokeKind>(), value => new AST.StrokeStyleAttribute(value), Format.Doc(
                "Controls the pattern used to draw lines.",
                Format.Names<StrokeKind>(),
                UseKind.Entity
            ));

            yield return Definition.Create("stroke", Attribute.Shorthand(Value.Colour, Value.Width, Identifier.Enum<StrokeKind>()), 
                                                     values => values switch { (var c, var w, var s) => new AST.StrokeShorthandAttribute(c, s, w) }, Format.Doc(
                "Shorthand for `stroke-colour`, `stroke-width` and `stroke-style`",
                "any of colour, width and/or style",
                UseKind.Entity,
                "stroke=hairline",
                "stroke=blue 3",
                "stroke=dashed #00f"
            ));
        }
    }
}
