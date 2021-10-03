using System.Collections.Generic;
using Thousand.Model;

namespace Thousand.Compose
{
    internal record RegionMeasurements(Point Size, IReadOnlyDictionary<IR.Object, NodeMeasurements> Nodes);
    internal record NodeMeasurements(int Row, int Column, int Margin, RegionMeasurements Region);
    internal record LineMeasurements(Point Position, Point Size, string Run);
    internal record BlockMeasurements(Point Size, LineMeasurements[] Lines);
}
