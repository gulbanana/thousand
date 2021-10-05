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
                new AST.ObjectClass("group", Array.Empty<string>(), new AST.ObjectAttribute[] 
                { 
                    new AST.NodeShapeAttribute(null), 
                    new AST.NodeLabelAttribute(null), 
                    new AST.RegionColumnWidthAttribute(new EqualSize()),
                    new AST.RegionRowHeightAttribute(new EqualSize())
                }),

                new AST.TypedObject(new[]{"big", "group"}, null, Array.Empty<AST.ObjectAttribute>(), new AST.ObjectDeclaration[] //uses larger font
                {
                    new AST.TypedObject(new[]{"object"}, "foo", Array.Empty<AST.ObjectAttribute>(), Array.Empty<AST.ObjectDeclaration>()),
                    new AST.TypedObject(new[]{"object"}, "bar", new AST.ObjectAttribute[] { new AST.TextFontSizeAttribute(40) }, Array.Empty<AST.ObjectDeclaration>()), // reduces font again
                }),

                new AST.TypedObject(new[]{"big"}, "baz", Array.Empty<AST.ObjectAttribute>(), Array.Empty<AST.ObjectDeclaration>()), //uses larger font
                new AST.TypedObject(new[]{"object"}, "qux", Array.Empty<AST.ObjectAttribute>(), Array.Empty<AST.ObjectDeclaration>()), 

                // chain containing two edges, both from foo to bar
                new AST.TypedLine(new[]{"line" }, new AST.LineSegment[]{ new("foo", ArrowKind.Forward), new("bar", ArrowKind.Backward), new("foo", null) }, new AST.SegmentAttribute[]{ })
            });

            var warnings = new List<GenerationError>();
            var errors = new List<GenerationError>();
            var result = Evaluator.TryEvaluate(new[] { document }, warnings, errors, out var root);

            Assert.True(result, errors.Join());
            Assert.Empty(warnings);
            Assert.Equal(Colour.Blue, root!.Region.Config.Fill);
            Assert.Equal(5, root.Region.WalkObjects().Count());
            Assert.Equal(3, root.Region.Objects.Count); // group, big baz
            Assert.Equal(2, root.Region.Objects[0].Region.Objects.Count); // group { foo, bar }
            Assert.Equal(2, root.Edges.Count);

            AssertEx.Sequence(root.Region.WalkObjects().Where(o => o.Label != null).Select(o => o.Font.Size), 50, 40, 50, 20);

            AssertEx.Sequence(root.Region.Objects.Select(o => o.Region.Config.ColumnWidth), new EqualSize(), new PackedSize(), new PackedSize());
        }
    }
}
