using System;
using System.Linq;
using Thousand.Evaluate;
using Thousand.Model;
using Xunit;
using static Thousand.Tests.Evaluation.DSL;

namespace Thousand.Tests.Evaluation
{
    public class Misc : IDisposable
    {
        private readonly GenerationState state = new();

        public void Dispose()
        {
            Assert.False(state.HasWarnings(), state.JoinWarnings());
        }

        [Fact]
        public void IntegrationTest()
        {
            var document = Document(
                OClass("object"),
                LClass("line"),
                OClass("big", new AST.TextFontSizeAttribute(50)), // increases font
                OClass("group",                
                    new AST.NodeShapeAttribute(null), 
                    new AST.EntityLabelAttribute(null, null, null), 
                    new AST.RegionLayoutColumnsAttribute(new EqualContentSize()),
                    new AST.RegionLayoutRowsAttribute(new EqualContentSize())
                ),

                Object(new[]{"big", "group"}, //uses larger font                
                    Object("object", "foo"),
                    Object("object", "bar", new AST.TextFontSizeAttribute(40)) // reduces font again
                ),

                Object("big", "baz"), //uses larger font
                Object("object", "qux"),

                // chain containing two edges, both from foo to bar
                Line("line", Segment("foo", ArrowKind.Forward), Segment("bar", ArrowKind.Backward), Segment("foo", null))
            );

            var result = Evaluator.TryEvaluate(new[] { document }, state, out var root);
            Assert.True(result, state.JoinErrors());

            Assert.Equal(5, root!.WalkNodes().Count());
            Assert.Equal(3, root!.Nodes.Count); // group, big baz
            Assert.Equal(2, root.Nodes[0].Region.Nodes.Count); // group { foo, bar }
            Assert.Equal(2, root.Edges.Count);

            AssertEx.Sequence(root.WalkNodes().Select(o => o.Label).WhereNotNull().Select(l => l.Font.Size), 50, 40, 50, 20);

            AssertEx.Sequence(root.Nodes.Select(o => o.Region.Config.Layout.Columns), new EqualContentSize(), new PackedSize(), new PackedSize());
        }

        [Fact]
        public void ScopeSibling()
        {
            var document = Document(
                OClass("object"),
                LClass("line"),
                Object("object", "foo"),
                Line("line", Segment("foo", ArrowKind.Neither), Segment("foo", null))
            );

            var result = Evaluator.TryEvaluate(new[] { document }, state, out var root);
            Assert.True(result, state.JoinErrors());

            Assert.Equal(root!.Edges[0].From.Target, root.Edges[0].To.Target);
        }

        [Fact]
        public void ScopeBubble()
        {
            var document = Document(
                OClass("object"),
                LClass("line"),
                Object("object",
                    Object("object", "foo")
                ),
                Line("line", Segment("foo", ArrowKind.Neither), Segment("foo", null))
            );

            var result = Evaluator.TryEvaluate(new[] { document }, state, out var root);
            Assert.True(result, state.JoinErrors());

            Assert.Equal(root!.Edges[0].From.Target, root.Edges[0].To.Target);
        }

        [Fact]
        public void ScopeShadow()
        {
            var document = Document(
                OClass("object"),
                LClass("line"),
                Object("object", "foo", new AST.NodeShapeAttribute(ShapeKind.Octagon)),
                Object("object",
                    Object("object", "foo", new AST.NodeShapeAttribute(ShapeKind.Circle)),
                    Line("line", Segment("foo", ArrowKind.Neither), Segment("foo", null))
                ),
                Line("line", Segment("foo", ArrowKind.Neither), Segment("foo", null))
            );

            var result = Evaluator.TryEvaluate(new[] { document }, state, out var root);
            Assert.True(result, state.JoinErrors());

            Assert.Equal(ShapeKind.Circle, root!.Nodes[1].Region.Edges[0].From.Target.Shape?.Style);
            Assert.Equal(ShapeKind.Octagon, root!.Edges[0].From.Target.Shape?.Style);
        }
    }
}
