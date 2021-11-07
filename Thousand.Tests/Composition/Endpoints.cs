using System;
using System.Linq;
using Thousand.Compose;
using Thousand.Model;
using Xunit;
using static Thousand.Tests.Composition.DSL;

namespace Thousand.Tests.Composition
{
    public class Endpoints : IDisposable
    {
        private readonly GenerationState state = new();

        public void Dispose()
        {
            Assert.False(state.HasWarnings(), state.JoinWarnings());
        }

        [Fact]
        public void LineOffset()
        {
            var left = Node() with { MinWidth = 10, MinHeight = 10 };
            var right = Node() with { MinWidth = 10, MinHeight = 10 };
            var edge = Edge(Endpoint(left, new Point(0, 1)), Endpoint(right, new Point(0, 1)));

            var root = Region(
                Config() with { Gutter = new(10) },
                left,
                right, 
                edge
            );

            var result = Composer.TryCompose(root, state, false, out var layout);

            Assert.True(result, state.JoinErrors());
            Assert.Single(layout!.Commands.OfType<Layout.Line>());
            Assert.Equal(new Point(10, 6), layout.Commands.OfType<Layout.Line>().Single().Start);
            Assert.Equal(new Point(20, 6), layout.Commands.OfType<Layout.Line>().Single().End);
        }

        [Fact]
        public void LineAnchor()
        {
            var left = Node() with { MinWidth = 10, MinHeight = 10 };
            var right = Node() with { MinWidth = 10, MinHeight = 10 };
            var edge = Edge(Endpoint(left, new CornerAnchor()), Endpoint(right, new AnyAnchor()));

            var root = Region(
                Config() with { Gutter = new(10) },
                left, 
                right, 
                edge
            );

            var result = Composer.TryCompose(root, state, false, out var layout);

            Assert.True(result, state.JoinErrors());
            Assert.Single(layout!.Commands.OfType<Layout.Line>());
            AssertEx.Eta(new Point(10, 0), layout.Commands.OfType<Layout.Line>().Single().Start); // XXX this can be 10,0 or 10,10 depending on the order of anchors; we need some sort of closest-to-other-anchor algorithm
            Assert.Equal(new Point(20, 5), layout.Commands.OfType<Layout.Line>().Single().End);
        }

        [Fact]
        public void LineAnchor_DiagonalRectangles()
        {
            var left = Node() with { MinWidth = 20, MinHeight = 10 };
            var right = Node() with { MinWidth = 20, MinHeight = 10, Row = 2, Column = 2 };
            var edge = Edge(Endpoint(left, new CornerAnchor()), Endpoint(right, new CornerAnchor()));

            var root = Region(
                Config() with { Gutter = new(10) },
                left, 
                right, 
                edge
            );

            var result = Composer.TryCompose(root, state, false, out var layout);
            Assert.True(result, state.JoinErrors());

            var line = layout!.Commands.OfType<Layout.Line>().Single();
            var shape1 = layout.Commands.OfType<Layout.Drawing>().ElementAt(0);
            var shape2 = layout.Commands.OfType<Layout.Drawing>().ElementAt(1);

            Assert.Equal(new Rect(0, 0, 20, 10), shape1.Bounds);
            AssertEx.Eta(new Point(20, 10), line.Start);

            Assert.Equal(new Rect(30, 20, 50, 30), shape2.Bounds);
            AssertEx.Eta(new Point(30, 20), line.End);
        }

        [Fact]
        public void LineAnchor_CenterToAnchor()
        {
            var left = Node() with { Shape = new(ShapeKind.Diamond), MinWidth = 50, MinHeight = 50 };
            var right = Node() with { Shape = new(ShapeKind.Rect), MinWidth = 150, MinHeight = 50, Row = 2, Column = 2 };
            var edge = Edge(Endpoint(left, new NoAnchor()), Endpoint(right, new CornerAnchor()));

            var root = Region(
                Config() with { Gutter = new(10) },
                left, 
                right, 
                edge
            );

            var result = Composer.TryCompose(root, state, false, out var layout);
            Assert.True(result, state.JoinErrors());

            var line = layout!.Commands.OfType<Layout.Line>().Single();
            var shape2 = layout.Commands.OfType<Layout.Drawing>().ElementAt(1);

            Assert.Equal(new Point(60, 60), shape2.Bounds.Origin);
            AssertEx.Eta(new Point(60, 60), line.End);
            AssertEx.Eta(new Point(37, 37), line.Start);
        }
    }
}
