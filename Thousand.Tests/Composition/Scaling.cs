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
            var unscaled = Object("unscaled") with { Shape = ShapeKind.Rect, MinWidth = 100, MinHeight = 100 };
            var scaled = Object("scaled") with { Shape = ShapeKind.Rect, MinWidth = 100, MinHeight = 100 };
            var line = Edge(Endpoint(unscaled, CompassKind.SE), Endpoint(scaled, CompassKind.SE));

            var root = Region(
                Object(unscaled) with { Position = Point.Zero },
                Object(Config() with { Scale = 2m }, scaled) with { Position = Point.Zero },
                line
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
            var unscaled = Object("unscaled") with { Shape = ShapeKind.Rect, MinWidth = 100, MinHeight = 100 };
            var scaled = Object("scaled") with { Shape = ShapeKind.Rect, MinWidth = 100, MinHeight = 100 };
            var line = Edge(Endpoint(unscaled, CompassKind.SE), Endpoint(scaled, CompassKind.SE));

            var root = Region(                
                Object(unscaled) with { Position = Point.Zero },
                Object(
                    Config() with { Scale = 2m }, 
                    scaled,
                    line
                ) with { Position = Point.Zero }
            );

            var result = Composer.TryCompose(root, state, out var layout);
            Assert.True(result, state.JoinErrors());

            var lineCommand = layout!.WalkCommands().OfType<Line>().Single();
            Assert.Equal(new Point(50, 50), lineCommand.Start);
            Assert.Equal(new Point(100, 100), lineCommand.End);
        }

        // XXX add tests: scaled line starts, scaled SE padding
    }
}
