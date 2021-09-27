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
                new IR.Config(1, Colour.White),
                new IR.Object[]
                {
                    new("foo", null, null, null, null, null, ShapeKind.RoundRect, 15, Colour.Black, Colour.White, 20f, null),
                    new("bar", null, null, null, null, null, ShapeKind.RoundRect, 15, Colour.Black, Colour.White, 20f, null)
                },
                new IR.Edge[] { }
            );

            var result = Composer.TryCompose(rules, new Dictionary<string, Point>(), warnings, errors, out var layout);

            Assert.True(result, errors.Join());
            Assert.Equal(300, layout!.Width);
            Assert.Equal(150, layout.Height);
        }

        [Fact]
        public void Layout1x2()
        {
            var rules = new IR.Rules(
                new IR.Config(1, Colour.White),
                new IR.Object[]
                {
                    new("foo", null, null, null, null, null, ShapeKind.RoundRect, 15, Colour.Black, Colour.White, 20f, null),
                    new("bar", null, 2, null, null, null,ShapeKind.RoundRect, 15, Colour.Black, Colour.White, 20f, null)
                },
                new IR.Edge[] { }
            );

            var result = Composer.TryCompose(rules, new Dictionary<string, Point>(), warnings, errors, out var layout);

            Assert.True(result, errors.Join());
            Assert.Equal(300, layout!.Width);
            Assert.Equal(150, layout.Height);
        }

        [Fact]
        public void Layout3x3Sparse()
        {
            var rules = new IR.Rules(
                new IR.Config(1, Colour.White),
                new IR.Object[]
                {
                    new("foo", 3, 3, null, null, null, ShapeKind.RoundRect, 15, Colour.Black, Colour.White, 20f, null)
                },
                new IR.Edge[] { }
            );

            var result = Composer.TryCompose(rules, new Dictionary<string, Point>(), warnings, errors, out var layout);

            Assert.True(result, errors.Join());
            Assert.Equal(450, layout!.Width);
            Assert.Equal(450, layout.Height);
        }
    }
}
