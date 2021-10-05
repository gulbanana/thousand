using System.Collections.Generic;
using Thousand.Model;

namespace Thousand.Compose
{
    internal record RowMeasurements(Point DesiredSize, int Margin, int Column) : NodeMeasurements(DesiredSize, Margin);

    internal sealed class RowState
    {
        public int ColumnCount { get; set; }
        public Dictionary<IR.Object, RowMeasurements> Nodes { get; } = new Dictionary<IR.Object, RowMeasurements>(ReferenceEqualityComparer.Instance);
    }
}
