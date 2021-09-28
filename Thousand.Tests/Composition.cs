using System;
using System.Collections.Generic;
using Thousand.Model;
using Xunit;

namespace Thousand.Tests
{

    public class Composition : IDisposable
    {
        private readonly List<GenerationError> warnings;
        private List<GenerationError> errors;
        private IReadOnlyDictionary<string, Point> measures;

        public Composition()
        {
            warnings = new List<GenerationError>();
            errors = new List<GenerationError>();
            measures = new MockMeasures(new Point(10, 10));
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
                    new IR.Object("foo") with { Padding = 0 },
                    new IR.Object("bar") with { Padding = 0 }
                },
                new IR.Edge[] { }
            );

            var result = Composer.TryCompose(rules, measures, warnings, errors, out var layout);

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
                    new IR.Object("foo") with { Padding = 0 },
                    new IR.Object("bar") with { Padding = 0, Row = 2 }
                },
                new IR.Edge[] { }
            );

            var result = Composer.TryCompose(rules, measures, warnings, errors, out var layout);

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
                    new IR.Object("foo") with { Padding = 0, Row = 1, Column = 1 },
                    new IR.Object("bar") with { Padding = 0, Row = 2, Column = 2 },
                    new IR.Object("baz") with { Padding = 0, Row = 3, Column = 3 },
                },
                new IR.Edge[] { }
            );

            var result = Composer.TryCompose(rules, measures, warnings, errors, out var layout);

            Assert.True(result, errors.Join());
            Assert.Equal(30, layout!.Width);
            Assert.Equal(30, layout.Height);
        }
    }
}
