using System.Collections.Generic;
using Thousand.Model;

namespace Thousand.Compose
{
    internal record GridMeasurements(Point DesiredSize, Border Margin, int Row, int Column) : NodeMeasurements(DesiredSize, Margin);

    internal sealed class GridState
    {
        public List<decimal> Columns { get; } = new List<decimal>();
        public List<decimal> Rows { get; } = new List<decimal>();
        public Dictionary<IR.Object, GridMeasurements> Nodes { get; } = new Dictionary<IR.Object, GridMeasurements>(ReferenceEqualityComparer.Instance);
    }
}
