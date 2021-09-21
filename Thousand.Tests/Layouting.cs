using System;
using Xunit;

namespace Thousand.Tests
{
    public class Composition
    {
        [Fact]
        public void CreateFromDiagram()
        {
            var document = new AST.Document(
                new AST.Node[] { new(new[]{"object"}, null, "foo", Array.Empty<AST.NodeAttribute>()), 
                new(new[]{"object"}, null, "bar", Array.Empty<AST.NodeAttribute>()) 
            });
            var result = Composer.TryCompose(document, out var layout, out var warnings, out var errors);

            Assert.Empty(warnings);
            Assert.True(result, errors.Join());
            Assert.Equal(300, layout!.Width);
            Assert.Equal(150, layout.Height);
        }
    }
}
