using System;
using System.Collections.Generic;
using System.Linq;
using Thousand.Model;
using Xunit;

namespace Thousand.Tests
{
    public class Evaluation
    {
        [Fact]
        public void IntegrationTest()
        {
            var document = new AST.Document(new AST.DocumentDeclaration[]
            {
                new AST.DiagramAttribute(new AST.RegionFillAttribute(Colour.Blue)),

                new AST.ObjectClass("object", Array.Empty<string>(), Array.Empty<AST.ObjectAttribute>()),
                new AST.LineClass("line", Array.Empty<string>(), Array.Empty<AST.SegmentAttribute>()),
                new AST.ObjectClass("big", Array.Empty<string>(), new AST.ObjectAttribute[] { new AST.TextFontSizeAttribute(50) }), // increases font

                new AST.TypedObject(new[]{"object"}, "foo", Array.Empty<AST.ObjectAttribute>(), Array.Empty<AST.ObjectDeclaration>()),
                new AST.TypedObject(new[]{"big"}, "bar", Array.Empty<AST.ObjectAttribute>(), Array.Empty<AST.ObjectDeclaration>()), //uses larger font
                new AST.TypedObject(new[]{"big"}, "baz", new AST.ObjectAttribute[] { new AST.TextFontSizeAttribute(40) }, Array.Empty<AST.ObjectDeclaration>()), // reduces font again

                // chain containing two edges, both from foo to bar
                new AST.TypedLine(new[]{"line" }, new AST.LineSegment[]{ new("foo", ArrowKind.Forward), new("bar", ArrowKind.Backward), new("foo", null) }, new AST.SegmentAttribute[]{ })
            });

            var warnings = new List<GenerationError>();
            var errors = new List<GenerationError>();
            var result = Evaluator.TryEvaluate(new[] { document }, warnings, errors, out var root);

            Assert.True(result, errors.Join());
            Assert.Empty(warnings);
            Assert.Equal(Colour.Blue, root!.Region.Config.Fill);
            Assert.Equal(3, root.Region.Objects.Count);
            Assert.Equal(2, root.Edges.Count);

            AssertEx.Sequence(root.Region.Objects.Select(o => o.Font.Size), 20, 50, 40);
        }
    }
}
