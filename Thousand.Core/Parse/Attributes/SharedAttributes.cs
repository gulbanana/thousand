using System.Collections.Generic;
using Thousand.Model;
using Definition = Thousand.Parse.Attributes.AttributeDefinition<Thousand.AST.SharedAttribute>;

namespace Thousand.Parse.Attributes
{
    // group used by both objects and lines
    static class SharedAttributes
    {
        public static IEnumerable<Definition> All()
        {
            yield return Definition.Create("stroke-colour", "stroke-color", Value.Colour, value => new AST.SharedStrokeColourAttribute(value), Format.Doc(
                "Controls the colour of lines. Supports HTML-style colours or a few well-known names.",
                "`#rrggbb` or `#rgb` (colour)",
                UseKind.Entity
            ));

            yield return Definition.Create("stroke-width", Value.Width, value => new AST.SharedStrokeWidthAttribute(value), Format.Doc(
                "Controls the presence and width of lines. Hairlines are always 1 pixel wide; lines with a set width scale with the document.",
                "`X` (number), `hairline` or `none`",
                UseKind.Entity
            ));

            yield return Definition.Create("stroke-style", Identifier.Enum<StrokeKind>(), value => new AST.SharedStrokeStyleAttribute(value), Format.Doc(
                "Controls the pattern used to draw lines.",
                Format.Names<StrokeKind>(),
                UseKind.Entity
            ));

            yield return Definition.Create("stroke", Attribute.Shorthand(Value.Colour, Value.Width, Identifier.Enum<StrokeKind>()), 
                                                     values => values switch { (var c, var w, var s) => new AST.SharedStrokeAttribute(c, s, w) }, Format.Doc(
                "Shorthand for `stroke-colour`, `stroke-width` and `stroke-style`",
                "any of colour, width and/or style",
                UseKind.Entity,
                "stroke=hairline",
                "stroke=blue 3",
                "stroke=dashed #00f"
            ));

            yield return Definition.Create("label-content", Value.String, value => new AST.SharedLabelContentAttribute(value), Format.Doc(
                "Text displayed inside an object or along a line. For an object, this is used instead of the object's name.",
                "`\"text\"` or `text` (string)",
                UseKind.Entity,
                "label-content=\"My Object\""
            ));

            yield return Definition.Create("label-justify", Attribute.AlignColumn, value => new AST.SharedLabelJustifyAttribute(value), Format.Doc(
                "Positions the label within the object.",
                "`start`/`left`, `center` or `end`/`right`",
                UseKind.Entity
            ));

            // XXX add label-offset

            yield return Definition.Create("label", Attribute.Shorthand(Identifier.Enum<AlignmentKind>(), Value.Text),
                                                    values => values switch { (var a, var t) => new AST.SharedLabelAttribute(t, a) }, Format.Doc(
                "Shorthand for `label-content` and `label-justify`.",
                "text and/or justification",
                UseKind.Entity,
                "label=\"My Object\"",
                "label=center",
                "label=boringobject end"
            ));
        }
    }
}
