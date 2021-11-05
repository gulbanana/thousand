using System.Collections.Generic;
using Thousand.Model;
using Thousand.Parse;

namespace Thousand.API
{
    // group used by both objects and lines
    static class EntityAttributes
    {
        private static readonly AttributeGroup<AST.EntityAttribute> Definition = new(UseKind.Entity);

        private static AttributeType<(Colour?, Width?, StrokeKind?)> StrokeShorthand { get; } = new(
            Attribute.ShorthandRRV(Value.Colour, Value.Width, Identifier.Enum<StrokeKind>()),
            "any of colour, width and/or style", 
            "hairline", "blue 3", "dashed #00f"
        );

        private static AttributeType<(Point?, AlignmentKind?, Text?)> LabelShorthand { get; } = new(
            Attribute.ShorthandRVV(Value.Point, Identifier.Enum<AlignmentKind>(), Value.Text),
            "text and/or justification", 
            "\"My Object\" 0 -10", "center", "boringobject end"
        );

        public static IEnumerable<AttributeDefinition<AST.EntityAttribute>> All()
        {
            yield return Definition.Create("stroke-colour", "stroke-color", AttributeType.Colour, value => new AST.EntityStrokeColourAttribute(value),
                "Controls the colour of lines. Supports HTML-style colours or a few well-known names."
            );

            yield return Definition.Create("stroke-width", AttributeType.Width, value => new AST.EntityStrokeWidthAttribute(value),
                "Controls the presence and width of lines. Hairlines are always 1 pixel wide; lines with a set width scale with the document."
            );

            yield return Definition.Create("stroke-style", AttributeType.Enum<StrokeKind>(), value => new AST.EntityStrokeStyleAttribute(value),
                "Controls the pattern used to draw lines."
            );

            yield return Definition.Create("stroke", StrokeShorthand, values => values switch { (var c, var w, var s) => new AST.EntityStrokeAttribute(c, s, w) }, 
                "Shorthand for `stroke-colour`, `stroke-width` and `stroke-style`."
            );

            yield return Definition.Create("label-content", AttributeType.String, value => new AST.EntityLabelContentAttribute(value),
                "Text displayed inside an object or along a line. For an object, this is used instead of the object's name."
            );

            yield return Definition.Create("label-align", AttributeType.AlignColumn, value => new AST.EntityLabelJustifyAttribute(value),
                "For objects, justifies the label text. For lines, positions the label before/on/after the line."
            );

            yield return Definition.Create("label-offset", AttributeType.PointRelative, value => new AST.EntityLabelOffsetAttribute(value),
                "Adjusts the position of the label."
            );

            yield return Definition.Create("label", LabelShorthand, values => values switch { (var offset, var align, var content) => new AST.EntityLabelAttribute(offset, content, align) },
                "Shorthand for `label-content`, `label-offset` and `label-align`."
            );
            
            yield return Definition.Create("anchor", AttributeType.Anchor.Twice(), startAndEnd => new AST.EntityAnchorAttribute(startAndEnd.first, startAndEnd.second), 
                "Defines the connection behaviour of the object/line, or of each line end separately."
            );

            yield return Definition.Create("offset", AttributeType.PointRelative.Twice(), startAndEnd => new AST.EntityOffsetAttribute(startAndEnd.first, startAndEnd.second),
                "Adjusts the position of the of the object/line, or of each line end separately."
            );
        }
    }
}
