using System;
using System.Collections.Generic;
using System.Linq;
using Thousand.Compose;
using Thousand.Model;
using Xunit;

namespace Thousand.Tests
{
    public class Composition : IDisposable
    {
        private readonly List<GenerationError> warnings;
        private List<GenerationError> errors;

        public Composition()
        {
            warnings = new List<GenerationError>();
            errors = new List<GenerationError>();
        }

        public void Dispose()
        {
            Assert.Empty(warnings);
        }

        [Fact]
        public void Layout2x1()
        {
            var rules = new IR.Rules(
                new IR.Config() with { Region = new IR.Region(LayoutKind.Grid, Margin: 0, Gutter: 0) },
                new IR.Object[]
                {
                    new IR.Object { Padding = 0, Width = 10, Height = 10 },
                    new IR.Object { Padding = 0, Width = 10, Height = 10 }
                },
                new IR.Edge[] { }
            );

            var result = Composer.TryCompose(rules, warnings, errors, out var layout);

            Assert.True(result, errors.Join());
            Assert.Equal(20, layout!.Width);
            Assert.Equal(10, layout.Height);
        }

        [Fact]
        public void Layout1x2()
        {
            var rules = new IR.Rules(
                new IR.Config() with { Region = new IR.Region(LayoutKind.Grid, Margin: 0, Gutter: 0) },
                new IR.Object[]
                {
                    new IR.Object { Padding = 0, Width = 10, Height = 10 },
                    new IR.Object { Padding = 0, Width = 10, Height = 10, Row = 2 }
                },
                new IR.Edge[] { }
            );

            var result = Composer.TryCompose(rules, warnings, errors, out var layout);

            Assert.True(result, errors.Join());
            Assert.Equal(10, layout!.Width);
            Assert.Equal(20, layout.Height);
        }

        [Fact]
        public void Layout3x3Sparse()
        {
            var rules = new IR.Rules(
                new IR.Config() with { Region = new IR.Region(LayoutKind.Grid, Margin: 0, Gutter: 0) },
                new IR.Object[]
                {
                    new IR.Object { Padding = 0, Width = 10, Height = 10, Row = 1, Column = 1 },
                    new IR.Object { Padding = 0, Width = 10, Height = 10, Row = 2, Column = 2 },
                    new IR.Object { Padding = 0, Width = 10, Height = 10, Row = 3, Column = 3 },
                },
                new IR.Edge[] { }
            );

            var result = Composer.TryCompose(rules, warnings, errors, out var layout);

            Assert.True(result, errors.Join());
            Assert.Equal(30, layout!.Width);
            Assert.Equal(30, layout.Height);
        }

        [Fact]
        public void LinePosition_Horizontal()
        {
            var left = new IR.Object { Shape = ShapeKind.Square, Padding = 0, Width = 10, Height = 10 };
            var right = new IR.Object { Shape = ShapeKind.Square, Padding = 0, Width = 10, Height = 10 };

            var rules = new IR.Rules(
                new IR.Config() with { Region = new IR.Region(LayoutKind.Grid, Margin: 0, Gutter: 10) },
                new[] { left, right },
                new[]
                {
                    new IR.Edge(left, right, null, null, Point.Zero, Point.Zero, new Stroke())
                }
            );

            var result = Composer.TryCompose(rules, warnings, errors, out var layout);

            Assert.True(result, errors.Join());
            Assert.Single(layout!.Lines);
            Assert.Equal(new Point(10, 5), layout.Lines.Single().Start);
            Assert.Equal(new Point(20, 5), layout.Lines.Single().End);
        }

        [Fact]
        public void LinePosition_45Degree()
        {
            var left = new IR.Object { Shape = ShapeKind.Square, Padding = 0, Width = 10, Height = 10 };
            var right = new IR.Object { Shape = ShapeKind.Square, Padding = 0, Width = 10, Height = 10, Row = 2, Column = 2 };

            var rules = new IR.Rules(
                new IR.Config() with { Region = new IR.Region(LayoutKind.Grid, Margin: 0, Gutter: 10) },
                new[] { left, right },
                new[]
                {
                    new IR.Edge(left, right, null, null, Point.Zero, Point.Zero, new Stroke())
                }
            );

            var result = Composer.TryCompose(rules, warnings, errors, out var layout);

            Assert.True(result, errors.Join());
            Assert.Single(layout!.Lines);
            AssertEx.Eta(new Point(10, 10), layout.Lines.Single().Start);
            AssertEx.Eta(new Point(20, 20), layout.Lines.Single().End);
        }

        [Fact]
        public void LineOffset()
        {
            var left = new IR.Object { Shape = ShapeKind.Square, Padding = 0, Width = 10, Height = 10 };
            var right = new IR.Object { Shape = ShapeKind.Square, Padding = 0, Width = 10, Height = 10 };

            var rules = new IR.Rules(
                new IR.Config() with { Region = new IR.Region(LayoutKind.Grid, Margin: 0, Gutter: 10) },
                new[] { left, right },
                new[]
                {
                    new IR.Edge(left, right, null, null, new Point(0, 1), new Point(0, 1), new Stroke())
                }
            );

            var result = Composer.TryCompose(rules, warnings, errors, out var layout);

            Assert.True(result, errors.Join());
            Assert.Single(layout!.Lines);
            Assert.Equal(new Point(10, 6), layout.Lines.Single().Start);
            Assert.Equal(new Point(20, 6), layout.Lines.Single().End);
        }

        [Fact]
        public void LineAnchor()
        {
            var left = new IR.Object { Shape = ShapeKind.Square, Padding = 0, Width = 10, Height = 10 };
            var right = new IR.Object { Shape = ShapeKind.Square, Padding = 0, Width = 10, Height = 10 };

            var rules = new IR.Rules(
                new IR.Config() with { Region = new IR.Region(LayoutKind.Grid, Margin: 0, Gutter: 10) },
                new[] { left, right },
                new[]
                {
                    new IR.Edge(left, right, AnchorKind.Corners, null, Point.Zero, Point.Zero, new Stroke())
                }
            );

            var result = Composer.TryCompose(rules, warnings, errors, out var layout);

            Assert.True(result, errors.Join());
            Assert.Single(layout!.Lines);
            AssertEx.Eta(new Point(10, 10), layout.Lines.Single().Start);
            Assert.Equal(new Point(20, 5), layout.Lines.Single().End);
        }
    }
}
