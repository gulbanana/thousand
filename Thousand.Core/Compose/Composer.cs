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
                var textMeasures = new Dictionary<StyledText, BlockMeasurements>();
                foreach (var t in from o in ir.WalkObjects() where o.Label is not null select new StyledText(o.Font, o.Label!))
                {
                    textMeasures[t] = Intrinsics.TextBlock(t);
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
        private readonly IReadOnlyDictionary<StyledText, BlockMeasurements> textMeasures;
        private readonly Dictionary<IR.Region, GridState> gridState;
        private readonly Dictionary<IR.Object, Layout.Shape> outputShapes;
        private readonly List<Layout.LabelBlock> outputLabels;
        private readonly List<Layout.Line> outputLines;

        private Composer(List<GenerationError> warnings, List<GenerationError> errors, IR.Root root, IReadOnlyDictionary<StyledText, BlockMeasurements> textMeasures)
        {
            ws = warnings;
            es = errors;

            this.root = root;
            this.textMeasures = textMeasures;

            gridState = new(ReferenceEqualityComparer.Instance);
            outputShapes = new(ReferenceEqualityComparer.Instance);
            outputLabels = new();
            outputLines = new();            
        }

        // XXX consider positioning the label in the region rather than overlaid 
        public Layout.Diagram Compose()
        {
            // measure hierarchical regions recursively
            var totalSize = Measure(root.Region, Point.Zero);

            // create labels and shapes, laid out according to the measurements above and each region's settings
            var boxes = Arrange(root.Region, Point.Zero);

            // create lines, which are global - all they need from layout is the margin-exclusive laid out bounds of each object 
            foreach (var edge in root.Edges)
            {
                var fromBox = boxes[edge.FromTarget];
                var toBox = boxes[edge.ToTarget];
                try
                {
                    var (start, end) = Intrinsics.Line(fromBox.Center + edge.FromOffset, toBox.Center + edge.ToOffset, outputShapes.GetValueOrDefault(edge.FromTarget), outputShapes.GetValueOrDefault(edge.ToTarget));

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
                            AnchorKind.Corners => outputShapes.GetValueOrDefault(edge.FromTarget) is Layout.Shape fromShape ? Intrinsics.Corners(fromShape) : Intrinsics.Corners(fromBox)
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
                            AnchorKind.Corners => outputShapes.GetValueOrDefault(edge.ToTarget) is Layout.Shape toShape ? Intrinsics.Corners(toShape) : Intrinsics.Corners(toBox)
                        };

                        end = end.Closest(anchors);
                    }

                    outputLines.Add(new(edge.Stroke, start, end, edge.FromMarker.HasValue, edge.ToMarker.HasValue));
                }
                catch (Exception ex)
                { 
                    ws.Add(new GenerationError(ex));
                    continue;
                }
            }

            return new(
                (int)Math.Ceiling(totalSize.X),
                (int)Math.Ceiling(totalSize.Y),
                root.Scale,
                root.Region.Config.Fill,
                outputShapes.Values.ToList(), 
                outputLabels, 
                outputLines
            );
        }

        // each object can have 0 or more of the following:
        // a) text content (a label)
        // b) child content (other objects)
        // c) a shape stroked around (and filled behind) the larger of (a) and (b)
        // all of these, plus padding, contribute to a object's bounding box size; a region (which can be the (b) of some other object, or the root) 
        // has a size determined by the object bounds, their margins and the region's gutter
        private Point Measure(IR.Region region, Point intrinsicSize)
        {
            var state = new GridState();
            gridState[region] = state;

            var currentRow = 1;
            var currentColumn = 1;

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

                // measure child and apply overrides
                var desiredSize = Measure(
                    child.Region, 
                    string.IsNullOrEmpty(child.Label) ? Point.Zero : textMeasures[new(child.Font, child.Label)].Size
                );

                if (child.MinWidth.HasValue)
                {
                    desiredSize = desiredSize with { X = Math.Max(child.MinWidth.Value, desiredSize.X) };
                }

                if (child.MinHeight.HasValue)
                {
                    desiredSize = desiredSize with { Y = Math.Max(child.MinHeight.Value, desiredSize.Y) };
                }

                if (child.Shape is ShapeKind.Square or ShapeKind.Roundsquare or ShapeKind.Circle or ShapeKind.Diamond)
                {
                    var longestSide = Math.Max(desiredSize.X, desiredSize.Y);
                    desiredSize = new Point(longestSide, longestSide);
                }

                state.Nodes[child] = new GridMeasurements(desiredSize, child.Margin, currentRow, currentColumn);

                // arrange: post-measure grid march
                state.RowCount = Math.Max(currentRow, state.RowCount);
                state.ColumnCount = Math.Max(currentColumn, state.ColumnCount);
                currentColumn++;
            }

            var intrinsicPadding = intrinsicSize == Point.Zero ? 0m : region.Config.Padding;
            var paddedIntrinsicSize = new Point(intrinsicSize.X + intrinsicPadding * 2, intrinsicSize.Y + intrinsicPadding * 2);

            var regionPadding = state.RowCount + state.ColumnCount == 0 ? 0m : region.Config.Padding;

            var rowHeights = new decimal[state.RowCount];
            var rowCenter = regionPadding;
            for (var r = 0; r < state.RowCount; r++)
            {
                var height = state.Nodes.Values.Where(s => s.Row == r + 1).Select(s => s.DesiredSize.Y + s.Margin * 2).Append(0).Max();
                var center = rowCenter + height / 2;
                rowCenter = rowCenter + height + region.Config.Gutter;
                rowHeights[r] = height;
            }

            var colWidths = new decimal[state.ColumnCount];
            var colCenter = regionPadding;
            for (var c = 0; c < state.ColumnCount; c++)
            {
                var width = state.Nodes.Values.Where(s => s.Column == c + 1).Select(s => s.DesiredSize.X + s.Margin * 2).Append(0).Max();
                var center = colCenter + width / 2;
                colCenter = colCenter + width + region.Config.Gutter;
                colWidths[c] = width;
            }

            var contentWidth = colWidths.Sum() + (state.ColumnCount - 1) * region.Config.Gutter + 2 * regionPadding;
            var contentHeight = rowHeights.Sum() + (state.RowCount - 1) * region.Config.Gutter + 2 * regionPadding;

            var regionSize = new Point(contentWidth, contentHeight);
            var contentSize = new Point(Math.Max(paddedIntrinsicSize.X, regionSize.X), Math.Max(paddedIntrinsicSize.Y, regionSize.Y));

            return contentSize;
        }

        private Dictionary<IR.Object, Rect> Arrange(IR.Region region, Point location)
        {
            var boxes = new Dictionary<IR.Object, Rect>(ReferenceEqualityComparer.Instance);
            var state = gridState[region];

            var rowCenters = new decimal[state.RowCount];
            var rowCenter = location.Y + region.Config.Padding;
            for (var r = 0; r < state.RowCount; r++)
            {
                var height = state.Nodes.Values.Where(s => s.Row == r + 1).Select(s => s.DesiredSize.Y + s.Margin * 2).Append(0).Max();
                var center = rowCenter + height / 2;
                rowCenter = rowCenter + height + region.Config.Gutter;
                rowCenters[r] = center;
            }

            var colCenters = new decimal[state.ColumnCount];
            var colCenter = location.X + region.Config.Padding;
            for (var c = 0; c < state.ColumnCount; c++)
            {
                var width = state.Nodes.Values.Where(s => s.Column == c + 1).Select(s => s.DesiredSize.X + s.Margin * 2).Append(0).Max();
                var center = colCenter + width / 2;
                colCenter = colCenter + width + region.Config.Gutter;
                colCenters[c] = center;
            }

            foreach (var obj in region.Objects)
            {
                var node = state.Nodes[obj];
                var center = new Point(colCenters[node.Column - 1], rowCenters[node.Row - 1]);
                var bounds = new Rect(node.DesiredSize).CenteredAt(center);

                boxes[obj] = bounds;

                if (obj.Shape.HasValue)
                {
                    var shape = new Layout.Shape(bounds, obj.Shape.Value, obj.CornerRadius, obj.Stroke, obj.Region.Config.Fill);
                    outputShapes[obj] = shape;
                }

                if (obj.Label != null && obj.Label != string.Empty)
                {
                    var block = textMeasures[new(obj.Font, obj.Label)];
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
                    outputLabels.Add(label);
                }

                if (obj.Region.Objects.Any())
                {
                    foreach (var kvp in Arrange(obj.Region, bounds.Origin))
                    {
                        boxes.Add(kvp.Key, kvp.Value);
                    }
                }
            }

            return boxes;
        }
    }
}
