using Thousand.Model;
using Xunit;

namespace Thousand.Tests
{
    public class Geometry
    {
        [Theory]
        [InlineData(0, 0, 0)]
        [InlineData(0, 1, 1)]
        [InlineData(1, 0, 1)]
        [InlineData(1, 1, 0)]
        [InlineData(1, 1, 1)]
        [InlineData(1, 2, 2)]
        [InlineData(2, 0, 0)]
        [InlineData(2, 1, 1)]
        [InlineData(2, 2, 2)]
        [InlineData(2, 3, 3)]
        [InlineData(3, 2, 2)]
        [InlineData(3, 3, 3)]
        public void PixelBoundaryCenteringStability(decimal diam, decimal x, decimal y)
        {
            var box1 = new Rect(new Point(diam, diam));
            var target = new Point(x, y);
            var box2 = box1.CenteredAt(target);

            Assert.Equal(target, box2.Center);
        }
    }
}
