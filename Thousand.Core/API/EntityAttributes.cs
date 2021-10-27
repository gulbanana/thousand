using System.Collections.Generic;
using Thousand.Model;
using Thousand.Parse;
using Definition = Thousand.API.AttributeDefinition<Thousand.AST.EntityAttribute>;

namespace Thousand.API
{
    // group used by both objects and lines
    static class EntityAttributes
    {
        public static IEnumerable<Definition> All()
        {
            yield return Definition.Create("stroke-colour", "stroke-color", Value.Colour, value => new AST.EntityStrokeColourAttribute(value),
                "Controls the colour of lines. Supports HTML-style colours or a few well-known names.",
                "`#rrggbb` or `#rgb` (colour)",
                UseKind.Entity
            );

            yield return Definition.Create("stroke-width", Value.Width, value => new AST.EntityStrokeWidthAttribute(value),
                "Controls the presence and width of lines. Hairlines are always 1 pixel wide; lines with a set width scale with the document.",
                "`X` (number), `hairline` or `none`",
                UseKind.Entity
            );

            yield return Definition.Create("stroke-style", Identifier.Enum<StrokeKind>(), value => new AST.EntityStrokeStyleAttribute(value),
                "Controls the pattern used to draw lines.",
                Format.Names<StrokeKind>(),
                UseKind.Entity
            );

            yield return Definition.Create("stroke", Attribute.ShorthandRRV(Value.Colour, Value.Width, Identifier.Enum<StrokeKind>()), 
                                                     values => values switch { (var c, var w, var s) => new AST.EntityStrokeAttribute(c, s, w) }, 
                "Shorthand for `stroke-colour`, `stroke-width` and `stroke-style`",
                "any of colour, width and/or style",
                UseKind.Entity,
                "stroke=hairline",
                "stroke=blue 3",
                "stroke=dashed #00f"
            );

            yield return Definition.Create("label-content", Value.String, value => new AST.EntityLabelContentAttribute(value),
                "Text displayed inside an object or along a line. For an object, this is used instead of the object's name.",
                "`\"text\"` or `text` (string)",
                UseKind.Entity,
                "label-content=\"My Object\""
            );

            yield return Definition.Create("label-align", Attribute.AlignColumn, value => new AST.EntityLabelJustifyAttribute(value),
                "For objects, justifies the label text. For lines, positions the label before/on/after the line.",
                "`start`/`left`, `center` or `end`/`right`",
                UseKind.Entity
            );

            yield return Definition.Create("label-offset", AttributeType.RelativePoint, value => new AST.EntityLabelOffsetAttribute(value),
                "Adjusts the position of the label.",
                UseKind.Entity
            );

            // XXX add label-offset

            yield return Definition.Create("label", Attribute.ShorthandRVV(Value.Point, Identifier.Enum<AlignmentKind>(), Value.Text),
                                                    values => values switch { (var offset, var align, var content) => new AST.EntityLabelAttribute(offset, content, align) },
                "Shorthand for `label-content`, `label-offset` and `label-align`.",
                "text and/or justification",
                UseKind.Entity,
                "label=\"My Object\" 0 -10",
                "label=center",
                "label=boringobject end"
            );
            
            // XXX write docs and types

            yield return Definition.Create("anchor", Value.Anchor.Twice(), startAndEnd => new AST.EntityAnchorAttribute(startAndEnd.first, startAndEnd.second));

            yield return Definition.Create("offset", Value.Point.Twice(), startAndEnd => new AST.EntityOffsetAttribute(startAndEnd.first, startAndEnd.second));
        }
    }
}
