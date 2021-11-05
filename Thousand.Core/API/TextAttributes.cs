using System.Collections.Generic;
using Thousand.Model;
using Thousand.Parse;

namespace Thousand.API
{
    static class TextAttributes
    {
        private static readonly AttributeGroup<AST.TextAttribute> Definition = new(null);

        private static AttributeType<(Colour?, string?, int?)> FontShorthand { get; } = new(
            Attribute.ShorthandRRV(Value.Colour, Value.String, Value.CountingNumber),
            "any or all of: colour, family, size",
            "hairline", "blue 3", "dashed #00f"
        );

        public static IEnumerable<AttributeDefinition<AST.TextAttribute>> All()
        {
            yield return Definition.Create("font-colour", "font-color", AttributeType.Colour, value => new AST.TextFontColourAttribute(value),
                "The colour applied to foreground text."
            );
            yield return Definition.Create("font-family", AttributeType.String, value => new AST.TextFontFamilyAttribute(value),
                "The name of any font installed on your computer (try \"San Francisco\" on Macs or \"Segoe UI\" on Windows."
            );
            yield return Definition.Create("font-size", AttributeType.PixelSize("X"), value => new AST.TextFontSizeAttribute(value),
                "Letters will be `X` pixels tall."
            );
            yield return Definition.Create("font", FontShorthand, values => values switch { (var c, var f, var s) => new AST.TextFontAttribute(f, s, c) },
                "Shorthand for font-colour, font-family and font-size."
            );
        }
    }
}
