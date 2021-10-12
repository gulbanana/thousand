using Thousand.Model;

namespace Thousand.Compose
{
    internal record LineMeasurements(Point Position, Point Size, string Run);
    internal record BlockMeasurements(Point Size, LineMeasurements[] Lines);
}
