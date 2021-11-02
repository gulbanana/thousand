using System;
using System.Linq;
using Thousand.Compose;
using Thousand.Layout;
using Thousand.Model;
using Xunit;
using static Thousand.Tests.Composition.DSL;

namespace Thousand.Tests.Composition
{
    public class Scaling : IDisposable
    {
        private readonly GenerationState state = new();

        public void Dispose()
        {
            Assert.False(state.HasWarnings(), state.JoinWarnings());
        }

        [Fact]
        public void BoxCorners_LineOutside()
        {
            var root = Region(
                Target(Object("unscaled") with { MinWidth = 100, MinHeight = 100 }),
                Object(
                    Config() with { Scale = 2m }, 
                    Target(Object("scaled") with { MinWidth = 100, MinHeight = 100 })
                ) with { Position = Point.Zero },
                Edge(Endpoint("unscaled", CompassKind.SE), Endpoint("scaled", CompassKind.SE))
            );

            var result = Composer.TryCompose(root, state, out var layout);
            Assert.True(result, state.JoinErrors());

            var lineCommand = layout!.WalkCommands().OfType<Line>().Single();
            Assert.Equal(new Point(100, 100), lineCommand.Start);
            Assert.Equal(new Point(200, 200), lineCommand.End);
        }

        [Fact]
        public void BoxCorners_LineWithin()
        {
            var root = Region(                
                Target(Object("unscaled") with { MinWidth = 100, MinHeight = 100 }),
                Object(
                    Config() with { Scale = 2m }, 
                    Target(Object("scaled") with { MinWidth = 100, MinHeight = 100 }),
                    Edge(Endpoint("unscaled", CompassKind.SE), Endpoint("scaled", CompassKind.SE))
                ) with { Position = Point.Zero }
            );

            var result = Composer.TryCompose(root, state, out var layout);
            Assert.True(result, state.JoinErrors());

            var lineCommand = layout!.WalkCommands().OfType<Line>().Single();
            Assert.Equal(new Point(50, 50), lineCommand.Start);
            Assert.Equal(new Point(100, 100), lineCommand.End);
        }

        [Fact]
        public void LineIntersects_NoScaling()
        {
            var root = Region(
                Object(
                    Config() with { Scale = 1m },
                    Target(Object("from") with { MinWidth = 100, MinHeight = 100 }),
                    Target(Object("to") with { MinWidth = 100, MinHeight = 100, Position = new Point(200, 0) }),
                    Edge("from", "to")
                )
            );

            var result = Composer.TryCompose(root, state, out var layout);
            Assert.True(result, state.JoinErrors());

            var lineCommand = layout!.WalkCommands().OfType<Line>().Single();
            Assert.Equal(new Point(100, 50), lineCommand.Start);
            Assert.Equal(new Point(200, 50), lineCommand.End);
        }

        [Fact]
        public void LineIntersects_LineOutside()
        {
            var root = Region(
                Object(
                    Config() with { Scale = 2m },
                    Target(Object("from") with { MinWidth = 100, MinHeight = 100 }),
                    Target(Object("to") with { MinWidth = 100, MinHeight = 100, Position = new Point(200, 0) })
                ),
                Edge("from", "to")
            );

            var result = Composer.TryCompose(root, state, out var layout);
            Assert.True(result, state.JoinErrors());

            var lineCommand = layout!.WalkCommands().OfType<Line>().Single();
            Assert.Equal(new Point(200, 100), lineCommand.Start);
            Assert.Equal(new Point(400, 100), lineCommand.End);
        }

        [Fact]
        public void LineIntersects_LineWithin()
        {
            var root = Region(
                Object(
                    Config() with { Scale = 2m },
                    Target(Object("from") with { MinWidth = 100, MinHeight = 100 }),
                    Target(Object("to") with { MinWidth = 100, MinHeight = 100, Position = new Point(200, 0) }),
                    Edge("from", "to")
                )
            );

            var result = Composer.TryCompose(root, state, out var layout);
            Assert.True(result, state.JoinErrors());

            var lineCommand = layout!.WalkCommands().OfType<Line>().Single();
            Assert.Equal(new Point(100, 50), lineCommand.Start);
            Assert.Equal(new Point(200, 50), lineCommand.End);
        }

        // XXX add tests: scaled SE padding
    }
}
