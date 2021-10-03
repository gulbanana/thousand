using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Thousand.Model;

namespace Thousand.Compose
{
    public class Composer
    {
        public static bool TryCompose(IR.Rules ir, List<GenerationError> warnings, List<GenerationError> errors, [NotNullWhen(true)] out Layout.Diagram? diagram)
        {
            try
            {
                var textMeasures = new Dictionary<string, BlockMeasurements>();
                foreach (var o in ir.Objects.Where(o => o.Label is not null))
                {
                    textMeasures[o.Label!] = Measure.TextBlock(o.Label!, o.Font, ir.Config.Scale);
                }

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
        private readonly IReadOnlyDictionary<string, BlockMeasurements> textMeasures;        
        private readonly Dictionary<IR.Object, Rect> boxes;
        private readonly Dictionary<IR.Object, Layout.Shape> shapes;
        private readonly List<Layout.LabelBlock> labels;
        private readonly List<Layout.Line> lines;

        private Composer(List<GenerationError> warnings, List<GenerationError> errors, IR.Rules rules, IReadOnlyDictionary<string, BlockMeasurements> textMeasures)
        {
            ws = warnings;
            es = errors;

            this.rules = rules;
            this.textMeasures = textMeasures;

            boxes = new(ReferenceEqualityComparer.Instance);
            shapes = new(ReferenceEqualityComparer.Instance);
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
            
            var sizes = new Dictionary<IR.Object, (int row, int col, Point size)>(ReferenceEqualityComparer.Instance);
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

                var size = MeasureIntrinsicSize(obj);
                sizes[obj] = (currentRow, currentColumn, size);

                maxRow = Math.Max(currentRow, maxRow);
                maxColumn = Math.Max(currentColumn, maxColumn);
                currentColumn++;
            }

            var rowHeights = new decimal[maxRow];
            var rowCenters = new decimal[maxRow];
            var rowCenter = 0m;
            for (var r = 0; r < maxRow; r++)
            {
                var height = sizes.Values.Where(s => s.row == r + 1).Select(s => s.size.Y).Append(0).Max();
                var center = rowCenter + height / 2 + rules.Config.Region.Margin;
                rowCenter = rowCenter + height + rules.Config.Region.Margin * 2 + rules.Config.Region.Gutter;
                rowHeights[r] = height;
                rowCenters[r] = center;
            }

            var colWidths = new decimal[maxColumn];
            var colCenters = new decimal[maxColumn];
            var colCenter = 0m;
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

                boxes[obj] = box;

                if (obj.Shape.HasValue)
                {
                    var shape = new Layout.Shape(box, obj.Shape.Value, obj.CornerRadius, obj.Stroke, obj.Fill);
                    shapes[obj] = shape;
                }
                
                if (obj.Label != null && obj.Label != string.Empty)
                {
                    var block = textMeasures[obj.Label];
                    var blockBox = new Rect(block.Size).CenteredAt(center);

                    var lines = new List<Layout.LabelLine>();
                    foreach (var line in block.Lines)
                    {
                        var lineBox = new Rect(blockBox.Origin + line.Position, line.Size);
                        lines.Add(new Layout.LabelLine(lineBox, line.Run));
                    }
                    var label = new Layout.LabelBlock(obj.Font, blockBox, obj.Label, lines);
                    labels.Add(label);
                }
            }

            foreach (var edge in rules.Edges)
            {
                var fromBox = boxes[edge.FromTarget];
                var toBox = boxes[edge.ToTarget];
                var (start, end) = Measure.Line(fromBox.Center + edge.FromOffset, toBox.Center + edge.ToOffset, shapes.GetValueOrDefault(edge.FromTarget), shapes.GetValueOrDefault(edge.ToTarget));

                if (edge.FromAnchor.HasValue)
                {
                    var anchors = edge.FromAnchor.Value switch
                    {
                        AnchorKind.CompassPoints => new Point[]
                        {
                            new(fromBox.Center.X, fromBox.Top),                            
                            new(fromBox.Right, fromBox.Center.Y),                            
                            new(fromBox.Center.X, fromBox.Bottom),
                            new(fromBox.Left, fromBox.Center.Y),
                        },
                        AnchorKind.Corners => shapes.GetValueOrDefault(edge.FromTarget) is Layout.Shape fromShape ? Measure.Corners(fromShape) : Measure.Corners(fromBox)
                    };

                    start = start.Closest(anchors);
                }

                if (edge.ToAnchor.HasValue)
                {
                    var anchors = edge.ToAnchor.Value switch
                    {
                        AnchorKind.CompassPoints => new Point[]
                        {
                            new(toBox.Center.X, toBox.Top),
                            new(toBox.Right, toBox.Center.Y),                            
                            new(toBox.Center.X, toBox.Bottom),
                            new(toBox.Left, toBox.Center.Y),
                        },
                        AnchorKind.Corners => shapes.GetValueOrDefault(edge.ToTarget) is Layout.Shape toShape ? Measure.Corners(toShape) : Measure.Corners(toBox)
                    };

                    end = end.Closest(anchors);
                }

                lines.Add(new(start, end, edge.Stroke));
            }

            return new(
                (int)Math.Ceiling(totalWidth),
                (int)Math.Ceiling(totalHeight),
                rules.Config.Scale,
                rules.Config.Background,
                shapes.Values.ToList(), 
                labels, 
                lines
            );
        }

        private Point MeasureIntrinsicSize(IR.Object obj)
        {
            Point size;

            if (obj.Label != null && obj.Label != string.Empty)
            {
                size = textMeasures[obj.Label].Size.Pad(obj.Padding);
            }
            else
            {
                size = Point.Zero;
            }

            if (obj.Width.HasValue || obj.Height.HasValue)
            {
                size = size with { X = obj.Width ?? size.X, Y = obj.Height ?? size.Y };
            }

            if (obj.Shape is ShapeKind.Square or ShapeKind.RoundSquare or ShapeKind.Circle or ShapeKind.Diamond)
            {
                var longestSide = Math.Max(size.X, size.Y);
                size = new Point(longestSide, longestSide);
            }

            return size;
        }
    }
}
