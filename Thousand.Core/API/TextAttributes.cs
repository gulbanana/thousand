using System.Collections.Generic;
using Thousand.Parse;
using Definition = Thousand.API.AttributeDefinition<Thousand.AST.TextAttribute>;

namespace Thousand.API
{
    // text group, used by everything - diagrams and objects cascade, objects and lines have labels
    static class TextAttributes
    {
        public static IEnumerable<Definition> All()
        {
            yield return Definition.Create("font-colour", "font-color", Value.Colour, value => new AST.TextFontColourAttribute(value));
            yield return Definition.Create("font-family", Value.String, value => new AST.TextFontFamilyAttribute(value));
            yield return Definition.Create("font-size", Value.CountingNumber, value => new AST.TextFontSizeAttribute(value));
            yield return Definition.Create("font", Attribute.ShorthandRRV(Value.Colour, Value.String, Value.CountingNumber), 
                                                   values => values switch { (var c, var f, var s) => new AST.TextFontAttribute(f, s, c) });
        }
    }
}
