using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Thousand.Model;

namespace Thousand
{
    public class Composer
    {
        public static bool TryCompose(IR.Rules ir, IReadOnlyDictionary<string, Point> textMeasures, List<GenerationError> warnings, List<GenerationError> errors, [NotNullWhen(true)] out Layout.Diagram? diagram)
        {
            try
            {
                var composition = new Composer(warnings, errors, ir, textMeasures);
                diagram = composition.Compose();

                return !errors.Any();
            }
            catch (Exception e)
            {
                errors.Add(new(e));
                diagram = null;
                return false;
            }
        }

        private readonly List<GenerationError> ws;
        private readonly List<GenerationError> es;
        private readonly IR.Rules rules;
        private readonly IReadOnlyDictionary<string, Point> textMeasures;        
        private readonly Dictionary<IR.Object, Layout.Shape> shapes;
        private readonly List<Layout.Label> labels;
        private readonly List<Layout.Line> lines;

        private Composer(List<GenerationError> warnings, List<GenerationError> errors, IR.Rules rules, IReadOnlyDictionary<string, Point> textMeasures)
        {
            ws = warnings;
            es = errors;

            this.rules = rules;
            this.textMeasures = textMeasures;            

            shapes = new();
            labels = new();
            lines = new();
        }

        public Layout.Diagram Compose()
        {
            // pass 1: measure
            var currentRow = 1;
            var maxRow = 1;
            var currentColumn = 1;
            var maxColumn = 1;
            
            var sizes = new Dictionary<IR.Object, (int row, int col, Point size)>();
            foreach (var obj in rules.Objects)
            {
                if (obj.Row.HasValue && currentRow != obj.Row.Value)
                {
                    currentRow = obj.Row.Value;
                    currentColumn = 1;
                }

                if (obj.Column.HasValue)
                {
                    currentColumn = obj.Column.Value;
                }

                var size = Measure(textMeasures, obj);
                sizes[obj] = (currentRow, currentColumn, size);

                maxRow = Math.Max(currentRow, maxRow);
                maxColumn = Math.Max(currentColumn, maxColumn);
                currentColumn++;
            }

            var rowHeights = new int[maxRow];
            var rowCenters = new int[maxRow];
            var rowCenter = 0;
            for (var r = 0; r < maxRow; r++)
            {
                var height = sizes.Values.Where(s => s.row == r + 1).Select(s => s.size.Y).Append(0).Max();
                var center = rowCenter + height / 2 + rules.Config.Region.Margin;
                rowCenter = rowCenter + height + rules.Config.Region.Margin * 2 + rules.Config.Region.Gutter;
                rowHeights[r] = height;
                rowCenters[r] = center;
            }

            var colWidths = new int[maxColumn];
            var colCenters = new int[maxColumn];
            var colCenter = 0;
            for (var c = 0; c < maxColumn; c++)
            {
                var width = sizes.Values.Where(s => s.col == c + 1).Select(s => s.size.X).Append(0).Max();
                var center = colCenter + width / 2 + rules.Config.Region.Margin;
                colCenter = colCenter + width + rules.Config.Region.Margin * 2 + rules.Config.Region.Gutter;
                colWidths[c] = width;
                colCenters[c] = center;
            }

            var totalWidth = colWidths.Sum() + (maxColumn * 2) * rules.Config.Region.Margin + (maxColumn - 1) * rules.Config.Region.Gutter;
            var totalHeight = rowHeights.Sum() + (maxRow * 2) * rules.Config.Region.Margin + (maxRow - 1) * rules.Config.Region.Gutter;

            // pass 2: arrange
            foreach (var obj in rules.Objects)
            {
                var measures = sizes[obj];
                var center = new Point(colCenters[measures.col-1], rowCenters[measures.row-1]);
                var box = new Rect(measures.size).CenteredAt(center);
                
                var shape = new Layout.Shape(obj.Name, obj.Kind, box, obj.CornerRadius, obj.Stroke, obj.Fill);
                shapes[obj] = shape;

                if (obj.Text != null && obj.Text.Label != string.Empty)
                {
                    var textBox = new Rect(textMeasures[obj.Text.Label]).CenteredAt(center);
                    var label = new Layout.Label(textBox, obj.Text.Label, obj.Text.FontSize);
                    labels.Add(label);
                }
            }

            foreach (var edge in rules.Edges)
            {
                // XXX this will pose a problem for shape=none
                var from = shapes[edge.FromTarget];
                var to = shapes[edge.ToTarget];

                lines.Add(new(from.Kind.HasValue ? from : null, to.Kind.HasValue ? to : null, from.Bounds.Center() + edge.FromOffset, to.Bounds.Center() + edge.ToOffset, edge.Stroke));
            }

            return new(
                totalWidth,
                totalHeight,
                rules.Config.Scale,
                rules.Config.Background,
                shapes.Values.Where(s => s.Kind.HasValue).ToList(), 
                labels, 
                lines
            );
        }

        private static Point Measure(IReadOnlyDictionary<string, Point> measures, IR.Object obj)
        {
            Point size;

            if (obj.Text != null && obj.Text.Label != string.Empty)
            {
                size = measures[obj.Text.Label].Pad(obj.Padding);
            }
            else
            {
                size = Point.Zero;
            }

            if (obj.Width.HasValue || obj.Height.HasValue)
            {
                size = size with { X = obj.Width ?? size.X, Y = obj.Height ?? size.Y };
            }

            if (obj.Kind is ShapeKind.Square or ShapeKind.RoundSquare or ShapeKind.Circle)
            {
                var longestSide = Math.Max(size.X, size.Y);
                size = new Point(longestSide, longestSide);
            }

            return size;
        }
    }
}
