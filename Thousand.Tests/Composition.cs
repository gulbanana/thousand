﻿using System;
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
            var rules = new IR.Root(
                new IR.Region(
                    new IR.Object { MinWidth = 10, MinHeight = 10 },
                    new IR.Object { MinWidth = 10, MinHeight = 10 }
                )
            );

            var result = Composer.TryCompose(rules, warnings, errors, out var layout);

            Assert.True(result, errors.Join());
            Assert.Equal(20, layout!.Width);
            Assert.Equal(10, layout.Height);
        }

        [Fact]
        public void Layout1x2()
        {
            var rules = new IR.Root(
                new IR.Region(
                    new IR.Object { MinWidth = 10, MinHeight = 10 },
                    new IR.Object { MinWidth = 10, MinHeight = 10, Row = 2 }
                )
            );

            var result = Composer.TryCompose(rules, warnings, errors, out var layout);

            Assert.True(result, errors.Join());
            Assert.Equal(10, layout!.Width);
            Assert.Equal(20, layout.Height);
        }

        [Fact]
        public void Layout3x3Sparse()
        {
            var rules = new IR.Root(
                new IR.Region(
                    new IR.Object { MinWidth = 10, MinHeight = 10, Row = 1, Column = 1 },
                    new IR.Object { MinWidth = 10, MinHeight = 10, Row = 2, Column = 2 },
                    new IR.Object { MinWidth = 10, MinHeight = 10, Row = 3, Column = 3 }
                )
            );

            var result = Composer.TryCompose(rules, warnings, errors, out var layout);

            Assert.True(result, errors.Join());
            Assert.Equal(30, layout!.Width);
            Assert.Equal(30, layout.Height);
        }

        [Fact]
        public void LinePosition_Horizontal()
        {
            var left = new IR.Object { Shape = ShapeKind.Square, MinWidth = 10, MinHeight = 10 };
            var right = new IR.Object { Shape = ShapeKind.Square, MinWidth = 10, MinHeight = 10 };

            var rules = new IR.Root(
                new IR.Region(
                    new IR.Config() with { Gutter = 10 },
                    new IR.Object[] { left, right }
                ),
                new IR.Edge(new Stroke(), left, right, null, null, null, null, Point.Zero, Point.Zero)
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
            var left = new IR.Object { Shape = ShapeKind.Square, MinWidth = 10, MinHeight = 10 };
            var right = new IR.Object { Shape = ShapeKind.Square, MinWidth = 10, MinHeight = 10, Row = 2, Column = 2 };

            var rules = new IR.Root(
                new IR.Region(
                    new IR.Config() with { Gutter = 10 },
                    new IR.Object[] { left, right }
                ),
                new IR.Edge(new Stroke(), left, right, null, null, null, null, Point.Zero, Point.Zero)
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
            var left = new IR.Object { Shape = ShapeKind.Square, MinWidth = 10, MinHeight = 10 };
            var right = new IR.Object { Shape = ShapeKind.Square, MinWidth = 10, MinHeight = 10 };

            var rules = new IR.Root(
                new IR.Region(
                    new IR.Config() with { Gutter = 10 },
                    new IR.Object[] { left, right }
                ),
                new IR.Edge(new Stroke(), left, right, null, null, null, null, new Point(0, 1), new Point(0, 1))
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
            var left = new IR.Object { Shape = ShapeKind.Square, MinWidth = 10, MinHeight = 10 };
            var right = new IR.Object { Shape = ShapeKind.Square, MinWidth = 10, MinHeight = 10 };

            var rules = new IR.Root(
                new IR.Region(
                    new IR.Config() with { Gutter = 10 },
                    new IR.Object[] { left, right }
                ),
                new IR.Edge(new Stroke(), left, right, null, null, AnchorKind.Corners, null, Point.Zero, Point.Zero)
            );

            var result = Composer.TryCompose(rules, warnings, errors, out var layout);

            Assert.True(result, errors.Join());
            Assert.Single(layout!.Lines);
            AssertEx.Eta(new Point(10, 10), layout.Lines.Single().Start);
            Assert.Equal(new Point(20, 5), layout.Lines.Single().End);
        }

        [Fact]
        public void PadText()
        {
            var rules = new IR.Root(
                new IR.Region(
                    new IR.Object(string.Empty) { Region = new IR.Region(new IR.Config { Padding = 1 }) },
                    new IR.Object("caption") { Region = new IR.Region(new IR.Config { Padding = 0 }) },
                    new IR.Object("caption") { Region = new IR.Region(new IR.Config { Padding = 1 }) },
                    new IR.Object("caption") { Region = new IR.Region(new IR.Config { Padding = 10 }) }
                )
            );

            var result = Composer.TryCompose(rules, warnings, errors, out var layout);
            Assert.True(result, errors.Join());

            var textSize = layout!.Labels[0].Bounds.Size;
            Assert.Equal(Point.Zero, layout.Shapes[0].Bounds.Size);
            Assert.Equal(textSize, layout.Shapes[1].Bounds.Size);
            Assert.Equal(textSize + new Point(2, 2), layout.Shapes[2].Bounds.Size);
            Assert.Equal(textSize + new Point(20, 20), layout.Shapes[3].Bounds.Size);
        }

        [Fact]
        public void PadChildren()
        {
            var rules = new IR.Root(
                new IR.Region(
                    new IR.Object 
                    (
                        new IR.Config { Padding = 1 }
                    ),
                    new IR.Object
                    (
                        new IR.Config { Padding = 0 },
                        new IR.Object { Shape = ShapeKind.Circle, MinWidth = 10, MinHeight = 10 }
                    ),
                    new IR.Object
                    (
                        new IR.Config { Padding = 0 },
                        new IR.Object { Shape = ShapeKind.Circle, MinWidth = 10, MinHeight = 10 },
                        new IR.Object { Shape = ShapeKind.Circle, MinWidth = 10, MinHeight = 10 }
                    ),
                    new IR.Object
                    (
                        new IR.Config { Padding = 1 },
                        new IR.Object { Shape = ShapeKind.Circle, MinWidth = 10, MinHeight = 10 }
                    ),
                    new IR.Object
                    (
                        new IR.Config { Padding = 1 },
                        new IR.Object { Shape = ShapeKind.Circle, MinWidth = 10, MinHeight = 10 },
                        new IR.Object { Shape = ShapeKind.Circle, MinWidth = 10, MinHeight = 10 }
                    )
                )
            );

            var result = Composer.TryCompose(rules, warnings, errors, out var layout);
            Assert.True(result, errors.Join());

            AssertEx.Sequence(layout!.Shapes.Where(s => s.Kind != ShapeKind.Circle).Select(s => s.Bounds.Size), 
                new Point(0, 0), 
                new Point(10, 10), 
                new Point(20, 10), 
                new Point(12, 12), 
                new Point(22, 12)
            );
        }
    }
}
