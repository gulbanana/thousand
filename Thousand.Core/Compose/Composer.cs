using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Thousand.Model;

namespace Thousand.Compose
{
    public class Composer
    {
        public static bool TryCompose(IR.Root ir, List<GenerationError> warnings, List<GenerationError> errors, [NotNullWhen(true)] out Layout.Diagram? diagram)
        {
            try
            {
                var textMeasures = new Dictionary<string, BlockMeasurements>();
                foreach (var o in ir.WalkObjects().Where(o => o.Label is not null))
                {
                    textMeasures[o.Label!] = Measure.TextBlock(o.Label!, o.Font);
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
        private readonly IR.Root root;
        private readonly IReadOnlyDictionary<string, BlockMeasurements> textMeasures;        
        private readonly Dictionary<IR.Object, Rect> boxes;
        private readonly Dictionary<IR.Object, Layout.Shape> shapes;
        private readonly List<Layout.LabelBlock> labels;
        private readonly List<Layout.Line> lines;

        private Composer(List<GenerationError> warnings, List<GenerationError> errors, IR.Root root, IReadOnlyDictionary<string, BlockMeasurements> textMeasures)
        {
            ws = warnings;
            es = errors;

            this.root = root;
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
            
            var sizes = new Dictionary<IR.Object, NodeMeasurements>(ReferenceEqualityComparer.Instance);
            foreach (var obj in root.WalkObjects()) // XXX should be recursive
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
                sizes[obj] = new NodeMeasurements(currentRow, currentColumn, size, obj.Margin);

                maxRow = Math.Max(currentRow, maxRow);
                maxColumn = Math.Max(currentColumn, maxColumn);
                currentColumn++;
            }

            var rowHeights = new decimal[maxRow];
            var rowCenters = new decimal[maxRow];
            var rowCenter = (decimal)root.Region.Config.Padding;
            for (var r = 0; r < maxRow; r++)
            {
                var height = sizes.Values.Where(s => s.row == r + 1).Select(s => s.size.Y + s.margin*2).Append(0).Max();
                var center = rowCenter + height / 2;
                rowCenter = rowCenter + height + root.Region.Config.Gutter;
                rowHeights[r] = height;
                rowCenters[r] = center;
            }

            var colWidths = new decimal[maxColumn];
            var colCenters = new decimal[maxColumn];
            var colCenter = (decimal)root.Region.Config.Padding;
            for (var c = 0; c < maxColumn; c++)
            {
                var width = sizes.Values.Where(s => s.col == c + 1).Select(s => s.size.X + s.margin*2).Append(0).Max();
                var center = colCenter + width / 2;
                colCenter = colCenter + width + root.Region.Config.Gutter;
                colWidths[c] = width;
                colCenters[c] = center;
            }

            var totalWidth = colWidths.Sum() + (maxColumn - 1) * root.Region.Config.Gutter + 2 * root.Region.Config.Padding;
            var totalHeight = rowHeights.Sum() + (maxRow - 1) * root.Region.Config.Gutter + 2 * root.Region.Config.Padding;

            // pass 2: arrange
            foreach (var obj in root.WalkObjects()) // XXX should be recursive
            {
                var measures = sizes[obj];
                var center = new Point(colCenters[measures.col-1], rowCenters[measures.row-1]);
                var box = new Rect(measures.size).CenteredAt(center);

                boxes[obj] = box;

                if (obj.Shape.HasValue)
                {
                    var shape = new Layout.Shape(box, obj.Shape.Value, obj.CornerRadius, obj.Stroke, obj.Region.Config.Fill);
                    shapes[obj] = shape;
                }
                
                if (obj.Label != null && obj.Label != string.Empty)
                {
                    var block = textMeasures[obj.Label];
                    var blockBox = new Rect(block.Size).CenteredAt(center);

                    // subpixel vertical positioning is not consistently supported in SVG
                    var pixelBoundary = blockBox.Top * root.Scale;
                    if (Math.Floor(pixelBoundary) != pixelBoundary)
                    {
                        blockBox = blockBox.Move(new Point(0, 0.5m));
                    }

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

            foreach (var edge in root.Edges)
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

                lines.Add(new(edge.Stroke, start, end, edge.FromMarker.HasValue, edge.ToMarker.HasValue));
            }

            return new(
                (int)Math.Ceiling(totalWidth),
                (int)Math.Ceiling(totalHeight),
                root.Scale,
                root.Region.Config.Fill,
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
                size = textMeasures[obj.Label].Size.Pad(obj.Region.Config.Padding);
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
