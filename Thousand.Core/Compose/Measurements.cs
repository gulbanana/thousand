using Thousand.Model;

namespace Thousand.Compose
{
    internal record NodeMeasurements(int row, int col, Point size, int margin);
    internal record LineMeasurements(Point Position, Point Size, string Run);
    internal record BlockMeasurements(Point Size, LineMeasurements[] Lines);
}
