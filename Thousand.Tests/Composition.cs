using System;
using System.Linq;
using Thousand.Compose;
using Thousand.Model;
using Xunit;

namespace Thousand.Tests
{
    public class Composition : IDisposable
    {
        private readonly GenerationState state = new();

        public void Dispose()
        {
            Assert.False(state.HasWarnings(), state.JoinWarnings());
        }

        [Fact]
        public void Layout2x1()
        {
            var root = new IR.Region(
                new IR.Object { MinWidth = 10, MinHeight = 10 },
                new IR.Object { MinWidth = 10, MinHeight = 10 }
            );

            var result = Composer.TryCompose(root, state, out var layout);

            Assert.True(result, state.JoinErrors());
            Assert.Equal(20, layout!.Width);
            Assert.Equal(10, layout.Height);
        }

        [Fact]
        public void Layout1x2()
        {
            var root = new IR.Region(
                new IR.Object { MinWidth = 10, MinHeight = 10 },
                new IR.Object { MinWidth = 10, MinHeight = 10, Row = 2 }
            );

            var result = Composer.TryCompose(root, state, out var layout);

            Assert.True(result, state.JoinErrors());
            Assert.Equal(10, layout!.Width);
            Assert.Equal(20, layout.Height);
        }

        [Fact]
        public void Layout3x3Sparse()
        {
            var root = new IR.Region(
                new IR.Object { MinWidth = 10, MinHeight = 10, Row = 1, Column = 1 },
                new IR.Object { MinWidth = 10, MinHeight = 10, Row = 2, Column = 2 },
                new IR.Object { MinWidth = 10, MinHeight = 10, Row = 3, Column = 3 }
            );

            var result = Composer.TryCompose(root, state, out var layout);

            Assert.True(result, state.JoinErrors());
            Assert.Equal(30, layout!.Width);
            Assert.Equal(30, layout.Height);
        }

        [Fact]
        public void LinePosition_Horizontal()
        {
            var left = new IR.Object { Shape = ShapeKind.Square, MinWidth = 10, MinHeight = 10 };
            var right = new IR.Object { Shape = ShapeKind.Square, MinWidth = 10, MinHeight = 10 };

            var root = new IR.Region(
                new IR.Config() with { Gutter = new(10) },
                new IR.Entity[] { left, right, new IR.Edge(left, right) }
            );

            var result = Composer.TryCompose(root, state, out var layout);

            Assert.True(result, state.JoinErrors());
            Assert.Single(layout!.Commands.OfType<Layout.Line>());
            Assert.Equal(new Point(10, 5), layout.Commands.OfType<Layout.Line>().Single().Start);
            Assert.Equal(new Point(20, 5), layout.Commands.OfType<Layout.Line>().Single().End);
        }

        [Fact]
        public void LinePosition_45Degree()
        {
            var left = new IR.Object { Shape = ShapeKind.Square, MinWidth = 10, MinHeight = 10 };
            var right = new IR.Object { Shape = ShapeKind.Square, MinWidth = 10, MinHeight = 10, Row = 2, Column = 2 };

            var root = new IR.Region(
                new IR.Config() with { Gutter = new(10) },
                new IR.Entity[] { left, right, new IR.Edge(left, right) }
            );

            var result = Composer.TryCompose(root, state, out var layout);

            Assert.True(result, state.JoinErrors());
            Assert.Single(layout!.Commands.OfType<Layout.Line>());
            AssertEx.Eta(new Point(10, 10), layout.Commands.OfType<Layout.Line>().Single().Start);
            AssertEx.Eta(new Point(20, 20), layout.Commands.OfType<Layout.Line>().Single().End);
        }

        [Fact]
        public void LineOffset()
        {
            var left = new IR.Object { Shape = ShapeKind.Square, MinWidth = 10, MinHeight = 10 };
            var right = new IR.Object { Shape = ShapeKind.Square, MinWidth = 10, MinHeight = 10 };
            var edge = new IR.Edge(new IR.Endpoint(left) with { Offset = new Point(0, 1) }, new IR.Endpoint(right) with { Offset = new Point(0, 1) });

            var root = new IR.Region(
                new IR.Config() with { Gutter = new(10) },
                new IR.Entity[] { left, right, edge }
            );

            var result = Composer.TryCompose(root, state, out var layout);

            Assert.True(result, state.JoinErrors());
            Assert.Single(layout!.Commands.OfType<Layout.Line>());
            Assert.Equal(new Point(10, 6), layout.Commands.OfType<Layout.Line>().Single().Start);
            Assert.Equal(new Point(20, 6), layout.Commands.OfType<Layout.Line>().Single().End);
        }

        [Fact]
        public void LineAnchor()
        {
            var left = new IR.Object { Shape = ShapeKind.Square, MinWidth = 10, MinHeight = 10 };
            var right = new IR.Object { Shape = ShapeKind.Square, MinWidth = 10, MinHeight = 10 };
            var edge = new IR.Edge(new IR.Endpoint(left) with { Anchor = new CornerAnchor() }, new IR.Endpoint(right) with { Anchor = new AnyAnchor() });

            var root = new IR.Region(
                new IR.Config() with { Gutter = new(10) },
                new IR.Entity[] { left, right, edge }
            );

            var result = Composer.TryCompose(root, state, out var layout);

            Assert.True(result, state.JoinErrors());
            Assert.Single(layout!.Commands.OfType<Layout.Line>());
            AssertEx.Eta(new Point(10, 0), layout.Commands.OfType<Layout.Line>().Single().Start); // XXX this can be 10,0 or 10,10 depending on the order of anchors; we need some sort of closest-to-other-anchor algorithm
            Assert.Equal(new Point(20, 5), layout.Commands.OfType<Layout.Line>().Single().End);
        }

        [Fact]
        public void LineAnchor_DiagonalRectangles()
        {
            var left = new IR.Object { Shape = ShapeKind.Rect, MinWidth = 20, MinHeight = 10 };
            var right = new IR.Object { Shape = ShapeKind.Rect, MinWidth = 20, MinHeight = 10, Row = 2, Column = 2 };
            var edge = new IR.Edge(new IR.Endpoint(left) with { Anchor = new CornerAnchor() }, new IR.Endpoint(right) with { Anchor = new CornerAnchor() });

            var root = new IR.Region(
                new IR.Config() with { Gutter = new(10) },
                new IR.Entity[] { left, right, edge }
            );

            var result = Composer.TryCompose(root, state, out var layout);
            Assert.True(result, state.JoinErrors());

            var line = layout!.Commands.OfType<Layout.Line>().Single();
            var shape1 = layout.Commands.OfType<Layout.Shape>().ElementAt(0);
            var shape2 = layout.Commands.OfType<Layout.Shape>().ElementAt(1);

            Assert.Equal(new Rect(0, 0, 20, 10), shape1.Bounds);
            AssertEx.Eta(new Point(20, 10), line.Start);

            Assert.Equal(new Rect(30, 20, 50, 30), shape2.Bounds);
            AssertEx.Eta(new Point(30, 20), line.End);
        }

        [Fact]
        public void LineAnchor_CenterToAnchor()
        {
            var left = new IR.Object { Shape = ShapeKind.Diamond, MinWidth = 50, MinHeight = 50 };
            var right = new IR.Object { Shape = ShapeKind.Rect, MinWidth = 150, MinHeight = 50, Row = 2, Column = 2 };
            var edge = new IR.Edge(new IR.Endpoint(left) with { Anchor = new NoAnchor() }, new IR.Endpoint(right) with { Anchor = new CornerAnchor() });

            var root = new IR.Region(
                new IR.Config() with { Gutter = new(10) },
                new IR.Entity[] { left, right, edge }
            );

            var result = Composer.TryCompose(root, state, out var layout);
            Assert.True(result, state.JoinErrors());

            var line = layout!.Commands.OfType<Layout.Line>().Single();
            var shape2 = layout.Commands.OfType<Layout.Shape>().ElementAt(1);

            Assert.Equal(new Point(60, 60), shape2.Bounds.Origin);
            AssertEx.Eta(new Point(60, 60), line.End);
            AssertEx.Eta(new Point(37, 37), line.Start);
        }

        [Fact]
        public void PadText()
        {
            var root = new IR.Region(
                new IR.Object(string.Empty) { Region = new IR.Region(new IR.Config { Padding = new(1) }) },
                new IR.Object("caption") { Region = new IR.Region(new IR.Config { Padding = new(0) }) },
                new IR.Object("caption") { Region = new IR.Region(new IR.Config { Padding = new(1) }) },
                new IR.Object("caption") { Region = new IR.Region(new IR.Config { Padding = new(10) }) }
            );

            var result = Composer.TryCompose(root, state, out var layout);
            Assert.True(result, state.JoinErrors());

            var textSize = layout!.Commands.OfType<Layout.Label>().ElementAt(0).Bounds.Size;
            Assert.Equal(new Point(2, 2), layout.Commands.OfType<Layout.Shape>().ElementAt(0).Bounds.Size);
            Assert.Equal(textSize, layout.Commands.OfType<Layout.Shape>().ElementAt(1).Bounds.Size);
            Assert.Equal(textSize + new Point(2, 2), layout.Commands.OfType<Layout.Shape>().ElementAt(2).Bounds.Size);
            Assert.Equal(textSize + new Point(20, 20), layout.Commands.OfType<Layout.Shape>().ElementAt(3).Bounds.Size);
        }

        [Fact]
        public void PadChildren()
        {
            var root = new IR.Region(
                new IR.Config { Padding = new(0, 0, 0, 1) },
                new IR.Object
                (
                    new IR.Config { Padding = new(1) }
                ),
                new IR.Object
                (
                    new IR.Config { Padding = new(0, 0, 0, 1) },
                    new IR.Object { Shape = ShapeKind.Circle, MinWidth = 10, MinHeight = 10 }
                ),
                new IR.Object
                (
                    new IR.Config { Padding = new(0) },
                    new IR.Object { Shape = ShapeKind.Circle, MinWidth = 10, MinHeight = 10 },
                    new IR.Object { Shape = ShapeKind.Circle, MinWidth = 10, MinHeight = 10 }
                ),
                new IR.Object
                (
                    new IR.Config { Padding = new(1) },
                    new IR.Object { Shape = ShapeKind.Circle, MinWidth = 10, MinHeight = 10 }
                ),
                new IR.Object
                (
                    new IR.Config { Padding = new(0, 1) },
                    new IR.Object { Shape = ShapeKind.Circle, MinWidth = 10, MinHeight = 10 },
                    new IR.Object { Shape = ShapeKind.Circle, MinWidth = 10, MinHeight = 10 }
                )
            );

            var result = Composer.TryCompose(root, state, out var layout);
            Assert.True(result, state.JoinErrors());

            Assert.Equal(new Point(64, 13), new Point(layout!.Width, layout.Height));
            AssertEx.Sequence(layout!.Commands.OfType<Layout.Shape>().Where(s => s.Kind != ShapeKind.Circle).Select(s => s.Bounds.Size),
                new Point(2, 2),
                new Point(10, 11),
                new Point(20, 10),
                new Point(12, 12),
                new Point(20, 12)
            );
        }
    }
}
