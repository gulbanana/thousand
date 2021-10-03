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
                    textMeasures[o.Label!] = Intrinsics.TextBlock(o.Label!, o.Font);
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
            var measures = Measure(root.Region, Point.Zero, null, null, false);
            Arrange(root.Region, measures, Point.Zero);

            foreach (var edge in root.Edges)
            {
                var fromBox = boxes[edge.FromTarget];
                var toBox = boxes[edge.ToTarget];
                try
                {
                    var (start, end) = Intrinsics.Line(fromBox.Center + edge.FromOffset, toBox.Center + edge.ToOffset, shapes.GetValueOrDefault(edge.FromTarget), shapes.GetValueOrDefault(edge.ToTarget));

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
                            AnchorKind.Corners => shapes.GetValueOrDefault(edge.FromTarget) is Layout.Shape fromShape ? Intrinsics.Corners(fromShape) : Intrinsics.Corners(fromBox)
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
                            AnchorKind.Corners => shapes.GetValueOrDefault(edge.ToTarget) is Layout.Shape toShape ? Intrinsics.Corners(toShape) : Intrinsics.Corners(toBox)
                        };

                        end = end.Closest(anchors);
                    }

                    lines.Add(new(edge.Stroke, start, end, edge.FromMarker.HasValue, edge.ToMarker.HasValue));
                }
                catch (Exception ex)
                { 
                    ws.Add(new GenerationError(ex));
                    continue;
                }
            }

            return new(
                (int)Math.Ceiling(measures.Size.X),
                (int)Math.Ceiling(measures.Size.Y),
                root.Scale,
                root.Region.Config.Fill,
                shapes.Values.ToList(), 
                labels, 
                lines
            );
        }

        // XXX assumes LayoutKind.Grid; mixes up measure and arrange passes
        private RegionMeasurements Measure(IR.Region region, Point intrinsicSize, int? finalWidth, int? finalHeight, bool squareBox)
        {
            var currentRow = 1;
            var maxRow = 0;
            var currentColumn = 1;
            var maxColumn = 0;

            var measures = new Dictionary<IR.Object, NodeMeasurements>(ReferenceEqualityComparer.Instance);
            foreach (var child in region.Objects)
            {
                // arrange: pre-measure grid march
                if (child.Row.HasValue && currentRow != child.Row.Value)
                {
                    currentRow = child.Row.Value;
                    currentColumn = 1;
                }

                if (child.Column.HasValue)
                {
                    currentColumn = child.Column.Value;
                }

                // measure                
                var childMeasurements = Measure(
                    child.Region, 
                    string.IsNullOrEmpty(child.Label) ? Point.Zero : textMeasures[child.Label].Size, 
                    child.Width, 
                    child.Height,
                    child.Shape is ShapeKind.Square or ShapeKind.RoundSquare or ShapeKind.Circle or ShapeKind.Diamond
                );

                measures[child] = new NodeMeasurements(currentRow, currentColumn, child.Margin, childMeasurements);

                // arrange: post-measure grid march
                maxRow = Math.Max(currentRow, maxRow);
                maxColumn = Math.Max(currentColumn, maxColumn);
                currentColumn++;
            }

            var rowHeights = new decimal[maxRow];
            var rowCenters = new decimal[maxRow]; // XXX not used, and recalculated later. save somewhere, or two-pass?
            var rowCenter = (decimal)region.Config.Padding;
            for (var r = 0; r < maxRow; r++)
            {
                var height = measures.Values.Where(s => s.Row == r + 1).Select(s => s.Region.Size.Y + s.Margin * 2).Append(0).Max();
                var center = rowCenter + height / 2;
                rowCenter = rowCenter + height + region.Config.Gutter;
                rowHeights[r] = height;
                rowCenters[r] = center;
            }

            var colWidths = new decimal[maxColumn];
            var colCenters = new decimal[maxColumn];
            var colCenter = (decimal)region.Config.Padding;
            for (var c = 0; c < maxColumn; c++)
            {
                var width = measures.Values.Where(s => s.Column == c + 1).Select(s => s.Region.Size.X + s.Margin * 2).Append(0).Max();
                var center = colCenter + width / 2;
                colCenter = colCenter + width + region.Config.Gutter;
                colWidths[c] = width;
                colCenters[c] = center;
            }

            var contentWidth = colWidths.Sum() + (maxColumn - 1) * region.Config.Gutter + 2 * region.Config.Padding;
            var contentHeight = rowHeights.Sum() + (maxRow - 1) * region.Config.Gutter + 2 * region.Config.Padding;

            var regionSize = new Point(contentWidth, contentHeight);
            var contentSize = new Point(Math.Max(intrinsicSize.X, regionSize.X), Math.Max(intrinsicSize.Y, regionSize.Y));
            var boxSize = contentSize + new Point(region.Config.Padding * 2, region.Config.Padding * 2);
            if (squareBox)
            {
                var longestSide = Math.Max(boxSize.X, boxSize.Y);
                boxSize = new Point(longestSide, longestSide);
            }
            var finalSize = new Point(finalWidth ?? boxSize.X, finalHeight ?? boxSize.Y);

            return new(finalSize, measures);
        }

        private void Arrange(IR.Region region, RegionMeasurements measures, Point location)
        {
            var maxRow = measures.Nodes.Values.Select(m => m.Row).Append(0).Max();
            var maxColumn = measures.Nodes.Values.Select(m => m.Column).Append(0).Max();

            var rowHeights = new decimal[maxRow];
            var rowCenters = new decimal[maxRow];
            var rowCenter = location.Y + region.Config.Padding;
            for (var r = 0; r < maxRow; r++)
            {
                var height = measures.Nodes.Values.Where(s => s.Row == r + 1).Select(s => s.Region.Size.Y + s.Margin * 2).Append(0).Max();
                var center = rowCenter + height / 2;
                rowCenter = rowCenter + height + region.Config.Gutter;
                rowHeights[r] = height;
                rowCenters[r] = center;
            }

            var colWidths = new decimal[maxColumn];
            var colCenters = new decimal[maxColumn];
            var colCenter = location.X + region.Config.Padding;
            for (var c = 0; c < maxColumn; c++)
            {
                var width = measures.Nodes.Values.Where(s => s.Column == c + 1).Select(s => s.Region.Size.X + s.Margin * 2).Append(0).Max();
                var center = colCenter + width / 2;
                colCenter = colCenter + width + region.Config.Gutter;
                colWidths[c] = width;
                colCenters[c] = center;
            }

            foreach (var obj in region.Objects)
            {
                var measure = measures.Nodes[obj];
                var center = new Point(colCenters[measure.Column - 1], rowCenters[measure.Row - 1]);
                var bounds = new Rect(measure.Region.Size).CenteredAt(center);

                boxes[obj] = bounds;

                if (obj.Shape.HasValue)
                {
                    var shape = new Layout.Shape(bounds, obj.Shape.Value, obj.CornerRadius, obj.Stroke, obj.Region.Config.Fill);
                    shapes[obj] = shape;
                }

                if (obj.Label != null && obj.Label != string.Empty)
                {
                    var block = textMeasures[obj.Label];
                    var blockBox = new Rect(block.Size).CenteredAt(bounds.Center);

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

                if (obj.Region.Objects.Any())
                {
                    Arrange(obj.Region, measure.Region, bounds.Origin);
                }
            }
        }
    }
}
