using System;
using Xunit;

namespace Thousand.Tests
{
    public class Composition
    {
        [Fact]
        public void CreateFromDiagram()
        {
            var document = new AST.Diagram(new AST.DiagramDeclaration[] 
            {
                new AST.Class("object", Array.Empty<string>(), Array.Empty<AST.ObjectAttribute>()),
                new AST.TypedObject(new[]{"object"}, null, "foo", Array.Empty<AST.ObjectAttribute>(), Array.Empty<AST.ObjectDeclaration>()), 
                new AST.TypedObject(new[]{"object"}, null, "bar", Array.Empty<AST.ObjectAttribute>(), Array.Empty<AST.ObjectDeclaration>())
            });
            var result = Composer.TryCompose(document, out var layout, out var warnings, out var errors);

            Assert.Empty(warnings);
            Assert.True(result, errors.Join());
            Assert.Equal(300, layout!.Width);
            Assert.Equal(150, layout.Height);
        }
    }
}
