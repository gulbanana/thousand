using System;
using System.Linq;
using Thousand.Compose;
using Thousand.Model;
using Xunit;
using static Thousand.Tests.Composition.DSL;

namespace Thousand.Tests.Composition
{
    public class Misc : IDisposable
    {
        private readonly GenerationState state = new();

        public void Dispose()
        {
            Assert.False(state.HasWarnings(), state.JoinWarnings());
        }

        [Fact]
        public void Layout2x1()
        {
            var root = Region(
                Object() with { MinWidth = 10, MinHeight = 10 },
                Object() with { MinWidth = 10, MinHeight = 10 }
            );

            var result = Composer.TryCompose(root, state, out var layout);

            Assert.True(result, state.JoinErrors());
            Assert.Equal(20, layout!.Width);
            Assert.Equal(10, layout.Height);
        }

        [Fact]
        public void Layout1x2()
        {
            var root = Region(
                Object() with { MinWidth = 10, MinHeight = 10 },
                Object() with { MinWidth = 10, MinHeight = 10, Row = 2 }
            );

            var result = Composer.TryCompose(root, state, out var layout);

            Assert.True(result, state.JoinErrors());
            Assert.Equal(10, layout!.Width);
            Assert.Equal(20, layout.Height);
        }

        [Fact]
        public void Layout3x3Sparse()
        {
            var root = Region(
                Object() with { MinWidth = 10, MinHeight = 10, Row = 1, Column = 1 },
                Object() with { MinWidth = 10, MinHeight = 10, Row = 2, Column = 2 },
                Object() with { MinWidth = 10, MinHeight = 10, Row = 3, Column = 3 }
            );

            var result = Composer.TryCompose(root, state, out var layout);

            Assert.True(result, state.JoinErrors());
            Assert.Equal(30, layout!.Width);
            Assert.Equal(30, layout.Height);
        }

        [Fact]
        public void LinePosition_Horizontal()
        {
            var left = Object() with { Shape = ShapeKind.Square, MinWidth = 10, MinHeight = 10 };
            var right = Object() with { Shape = ShapeKind.Square, MinWidth = 10, MinHeight = 10 };

            var root = Region(
                Config() with { Gutter = new(10) },
                left, 
                right, 
                Edge(left, right)
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
            var left = Object() with { Shape = ShapeKind.Square, MinWidth = 10, MinHeight = 10 };
            var right = Object() with { Shape = ShapeKind.Square, MinWidth = 10, MinHeight = 10, Row = 2, Column = 2 };

            var root = Region(
                Config() with { Gutter = new(10) },
                left, 
                right, 
                Edge(left, right)
            );

            var result = Composer.TryCompose(root, state, out var layout);

            Assert.True(result, state.JoinErrors());
            Assert.Single(layout!.Commands.OfType<Layout.Line>());
            AssertEx.Eta(new Point(10, 10), layout.Commands.OfType<Layout.Line>().Single().Start);
            AssertEx.Eta(new Point(20, 20), layout.Commands.OfType<Layout.Line>().Single().End);
        }

        [Fact]
        public void PadText()
        {
            var root = Region(
                Object(Config() with { Padding = new(1) }) with { Label = Label(string.Empty) },
                Object(Config() with { Padding = new(0) }) with { Label = Label("caption") },
                Object(Config() with { Padding = new(1) }) with { Label = Label("caption") },
                Object(Config() with { Padding = new(10) }) with { Label = Label("caption") }
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
            var root = Region(
                Config() with { Padding = new(0, 0, 0, 1) },
                Object
                (
                    Config() with { Padding = new(1) }
                ),
                Object
                (
                    Config() with { Padding = new(0, 0, 0, 1) },
                    Object() with { Shape = ShapeKind.Circle, MinWidth = 10, MinHeight = 10 }
                ),
                Object
                (
                    Config() with { Padding = new(0) },
                    Object() with { Shape = ShapeKind.Circle, MinWidth = 10, MinHeight = 10 },
                    Object() with { Shape = ShapeKind.Circle, MinWidth = 10, MinHeight = 10 }
                ),
                Object
                (
                    Config() with { Padding = new(1) },
                    Object() with { Shape = ShapeKind.Circle, MinWidth = 10, MinHeight = 10 }
                ),
                Object
                (
                    Config() with { Padding = new(0, 1) },
                    Object() with { Shape = ShapeKind.Circle, MinWidth = 10, MinHeight = 10 },
                    Object() with { Shape = ShapeKind.Circle, MinWidth = 10, MinHeight = 10 }
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
