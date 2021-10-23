using System.Collections.Generic;
using Definition = Thousand.Parse.Attributes.AttributeDefinition<Thousand.AST.TextAttribute>;

namespace Thousand.Parse.Attributes
{
    // text group, used only by objects (so far)
    static class TextAttributes
    {
        public static IEnumerable<Definition> All()
        {
            yield return Definition.Create("font-colour", "font-color", Value.Colour, value => new AST.TextFontColourAttribute(value));
            yield return Definition.Create("font-family", Value.String, value => new AST.TextFontFamilyAttribute(value));
            yield return Definition.Create("font-size", Value.CountingNumber, value => new AST.TextFontSizeAttribute(value));
            yield return Definition.Create("font", Attribute.Shorthand(Value.Colour, Value.String, Value.CountingNumber), 
                                                   values => values switch { (var c, var f, var s) => new AST.TextFontAttribute(f, s, c) });
        }
    }
}
