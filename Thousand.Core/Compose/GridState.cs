using System.Collections.Generic;
using Thousand.Model;

namespace Thousand.Compose
{
    internal record GridMeasurements(Point DesiredSize, int Margin, int Row, int Column) : NodeMeasurements(DesiredSize, Margin);

    internal sealed class GridState
    {
        public int ColumnCount { get; set; }
        public int RowCount { get; set; }
        public Dictionary<IR.Object, GridMeasurements> Nodes { get; } = new Dictionary<IR.Object, GridMeasurements>(ReferenceEqualityComparer.Instance);
    }
}
