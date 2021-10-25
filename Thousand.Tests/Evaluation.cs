using System;
using System.Linq;
using Thousand.Evaluate;
using Thousand.Model;
using Xunit;

namespace Thousand.Tests
{
    public class Evaluation : IDisposable
    {
        private readonly GenerationState state = new();

        public void Dispose()
        {
            Assert.False(state.HasWarnings(), state.JoinWarnings());
        }

        [Fact]
        public void IntegrationTest()
        {
            var document = new AST.TypedDocument(new AST.TypedDocumentContent[]
            {
                new AST.DocumentAttribute(new AST.RegionFillAttribute(Colour.Blue)),

                new AST.ObjectClass("object"),
                new AST.LineClass("line"),
                new AST.ObjectClass("big", new AST.TextFontSizeAttribute(50)), // increases font
                new AST.ObjectClass("group",                
                    new AST.NodeShapeAttribute(null), 
                    new AST.NodeLabelContentAttribute(new Text()), 
                    new AST.RegionLayoutColumnsAttribute(new EqualContentSize()),
                    new AST.RegionLayoutRowsAttribute(new EqualContentSize())
                ),

                new AST.TypedObject(new Parse.Identifier[]{new("big"), new("group")}, null, Array.Empty<AST.ObjectAttribute>(), new AST.TypedObjectContent[] //uses larger font
                {
                    new AST.TypedObject("object", "foo"),
                    new AST.TypedObject("object", "bar", new AST.TextFontSizeAttribute(40)), // reduces font again
                }),

                new AST.TypedObject("big", "baz"), //uses larger font
                new AST.TypedObject("object", "qux"), 

                // chain containing two edges, both from foo to bar
                new AST.TypedLine("line", new AST.LineSegment<AST.TypedObject>[]{ new("foo", ArrowKind.Forward), new("bar", ArrowKind.Backward), new("foo", null) })
            });

            var result = Evaluator.TryEvaluate(new[] { document }, state, out var root);
            Assert.True(result, state.JoinErrors());

            Assert.Equal(Colour.Blue, root!.Region.Config.Fill);
            Assert.Equal(5, root.Region.WalkObjects().Count());
            Assert.Equal(3, root.Region.Objects.Count); // group, big baz
            Assert.Equal(2, root.Region.Objects[0].Region.Objects.Count); // group { foo, bar }
            Assert.Equal(2, root.Edges.Count);

            AssertEx.Sequence(root.Region.WalkObjects().Select(o => o.Label).WhereNotNull().Select(l => l.Font.Size), 50, 40, 50, 20);

            AssertEx.Sequence(root.Region.Objects.Select(o => o.Region.Config.Layout.Columns), new EqualContentSize(), new PackedSize(), new PackedSize());
        }

        [Fact]
        public void ScopeSibling()
        {
            var document = new AST.TypedDocument(new AST.TypedDocumentContent[]
            {
                new AST.ObjectClass("object"),
                new AST.LineClass("line"),
                new AST.TypedObject("object", "foo"),
                new AST.TypedLine("line", new("foo", ArrowKind.Neither), new("foo", null))
            });

            var result = Evaluator.TryEvaluate(new[] { document }, state, out var root);
            Assert.True(result, state.JoinErrors());

            Assert.Equal(root!.Edges[0].FromTarget, root.Edges[0].ToTarget);
        }

        [Fact]
        public void ScopeBubble()
        {
            var document = new AST.TypedDocument(new AST.TypedDocumentContent[]
            {
                new AST.ObjectClass("object"),
                new AST.LineClass("line"),
                new AST.TypedObject("object", null, Array.Empty<AST.ObjectAttribute>(),
                    new AST.TypedObject("object", "foo")
                ),                
                new AST.TypedLine("line", new("foo", ArrowKind.Neither), new("foo", null))
            });

            var result = Evaluator.TryEvaluate(new[] { document }, state, out var root);
            Assert.True(result, state.JoinErrors());

            Assert.Equal(root!.Edges[0].FromTarget, root.Edges[0].ToTarget);
        }

        [Fact]
        public void ScopeShadow()
        {
            var document = new AST.TypedDocument(new AST.TypedDocumentContent[]
            {
                new AST.ObjectClass("object"),
                new AST.LineClass("line"),
                new AST.TypedObject("object", "foo", new AST.NodeShapeAttribute(ShapeKind.Octagon)),
                new AST.TypedObject("object", null, Array.Empty<AST.ObjectAttribute>(),
                    new AST.TypedObject("object", "foo", new AST.NodeShapeAttribute(ShapeKind.Circle)),
                    new AST.TypedLine("line", new("foo", ArrowKind.Neither), new("foo", null))
                ),
                new AST.TypedLine("line", new("foo", ArrowKind.Neither), new("foo", null))
            });

            var result = Evaluator.TryEvaluate(new[] { document }, state, out var root);
            Assert.True(result, state.JoinErrors());

            Assert.Equal(ShapeKind.Circle, root!.Edges[0].FromTarget.Shape);
            Assert.Equal(ShapeKind.Octagon, root!.Edges[1].FromTarget.Shape);
        }
    }
}
