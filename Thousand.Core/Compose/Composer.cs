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
            catch (Exception ex)
            {
                errors.Add(new(ex));
                diagram = null;
                return false;
            }
        }

        private readonly List<GenerationError> ws;
        private readonly List<GenerationError> es;
        private readonly IR.Root root;
        private readonly IReadOnlyDictionary<StyledText, BlockMeasurements> textMeasures;
        private readonly Dictionary<IR.Region, GridState> gridState;
        private readonly Dictionary<IR.Region, RowState> rowState;
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
            rowState = new(ReferenceEqualityComparer.Instance);

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

                    if (edge.FromTarget.Shape.HasValue)
                    {
                        var anchors = Shapes.Anchors(edge.FromTarget.Shape.Value, edge.FromTarget.CornerRadius, fromBox);

                        switch (edge.FromAnchor)
                        {
                            case AnyAnchor:
                                start = start.ClosestTo(anchors.Values.Select(c => c.Location));
                                break;

                            case CornerAnchor:
                                start = start.ClosestTo(anchors.Values.Where(c => c.IsCorner).Select(c => c.Location));
                                break;

                            case SpecificAnchor(var dir):
                                start = anchors.GetValueOrDefault(dir)?.Location ?? start;
                                break;
                        }

                        if (edge.FromAnchor is not NoAnchor && edge.ToAnchor is NoAnchor)
                        {
                            (_, end) = Intrinsics.Line(start, toBox.Center + edge.ToOffset, null, outputShapes.GetValueOrDefault(edge.ToTarget));
                        }
                    }

                    if (edge.ToTarget.Shape.HasValue)
                    {
                        var anchors = Shapes.Anchors(edge.ToTarget.Shape.Value, edge.ToTarget.CornerRadius, toBox);

                        switch (edge.ToAnchor)
                        {
                            case AnyAnchor:
                                end = end.ClosestTo(anchors.Values.Select(c => c.Location));
                                break;

                            case CornerAnchor:
                                end = end.ClosestTo(anchors.Values.Where(c => c.IsCorner).Select(c => c.Location));
                                break;

                            case SpecificAnchor(var dir):
                                end = anchors.GetValueOrDefault(dir)?.Location ?? end;
                                break;
                        }

                        if (edge.ToAnchor is not NoAnchor && edge.FromAnchor is NoAnchor)
                        {
                            (start, _) = Intrinsics.Line(fromBox.Center + edge.FromOffset, end, outputShapes.GetValueOrDefault(edge.FromTarget), null);
                        }
                    }

                    outputLines.Add(new(edge.Stroke, start, end, edge.FromMarker.HasValue, edge.ToMarker.HasValue));
                }
                catch (Exception ex)
                { 
                    ws.Add(new(ex));
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
                // grid-march: reset to manually specified cell
                if (region.Config is { Layout: LayoutKind.Column } or { Layout: LayoutKind.Grid, Flow: FlowKind.Row })
                {
                    if (child.Row.HasValue && currentRow != child.Row.Value)
                    {
                        currentRow = child.Row.Value;
                        currentColumn = 1;
                    }
                    currentColumn = child.Column ?? currentColumn;
                }

                if (region.Config is { Layout: LayoutKind.Row } or { Layout: LayoutKind.Grid, Flow: FlowKind.Column })
                {
                    if (child.Column.HasValue && currentColumn != child.Column.Value)
                    {
                        currentColumn = child.Column.Value;
                        currentRow = 1;
                    }
                    currentRow = child.Row ?? currentRow;
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

                // grid-march: update size and move to the next cell
                state.RowCount = Math.Max(currentRow, state.RowCount);
                state.ColumnCount = Math.Max(currentColumn, state.ColumnCount);

                if (region.Config is { Layout: LayoutKind.Row } or { Layout: LayoutKind.Grid, Flow: FlowKind.Row })
                {
                    currentColumn++;
                }
                else if (region.Config is { Layout: LayoutKind.Column } or { Layout: LayoutKind.Grid, Flow: FlowKind.Column })
                {
                    currentRow++;
                }
            }

            var intrinsicPadding = intrinsicSize == Point.Zero ? new Border(0) : region.Config.Padding;
            var paddedIntrinsicSize = new Point(intrinsicSize.X + intrinsicPadding.X, intrinsicSize.Y + intrinsicPadding.Y);

            var regionPadding = state.RowCount + state.ColumnCount == 0 ? new Border(0) : region.Config.Padding;

            var colWidths = new decimal[state.ColumnCount];
            var colCenter = regionPadding.Left;
            for (var c = 0; c < state.ColumnCount; c++)
            {
                var width = state.Nodes.Values.Where(s => s.Column == c + 1).Select(s => s.DesiredSize.X + s.Margin.X).Append(0).Max();
                colCenter = colCenter + width + region.Config.Gutter.Columns;
                colWidths[c] = width;
            }

            var rowHeights = new decimal[state.RowCount];
            var rowCenter = regionPadding.Top;
            for (var r = 0; r < state.RowCount; r++)
            {
                var height = state.Nodes.Values.Where(s => s.Row == r + 1).Select(s => s.DesiredSize.Y + s.Margin.Y).Append(0).Max();
                rowCenter = rowCenter + height + region.Config.Gutter.Rows;
                rowHeights[r] = height;
            }

            var contentWidth = colWidths.Sum() + (state.ColumnCount - 1) * region.Config.Gutter.Columns + regionPadding.X;
            var contentHeight = rowHeights.Sum() + (state.RowCount - 1) * region.Config.Gutter.Rows + regionPadding.Y;

            var regionSize = new Point(contentWidth, contentHeight);
            var contentSize = new Point(Math.Max(paddedIntrinsicSize.X, regionSize.X), Math.Max(paddedIntrinsicSize.Y, regionSize.Y));

            return contentSize;
        }

        // lays out a region's contents at a given point - size is known to parents, so it's just a matter of dividing up the space
        // paint back-to-front: shape, then intrinsic content, then children
        private Dictionary<IR.Object, Rect> Arrange(IR.Region region, Point location)
        {
            var boxes = new Dictionary<IR.Object, Rect>(ReferenceEqualityComparer.Instance);
            var state = gridState[region];

            var columns = new Track[state.ColumnCount];
            var colMarker = location.X + region.Config.Padding.Left;
            for (var c = 0; c < state.ColumnCount; c++)
            {
                var width = state.Nodes.Values.Where(n => n.Column == c + 1).Select(n => n.DesiredSize.X + n.Margin.X).Append(0).Max();
                
                var start = colMarker;
                var center = colMarker + width / 2;
                colMarker = colMarker + width + region.Config.Gutter.Columns;
                var end = colMarker;

                columns[c] = new(start, center, end);
            }

            var rows = new Track[state.RowCount];
            var rowMarker = location.Y + region.Config.Padding.Top;
            for (var r = 0; r < state.RowCount; r++)
            {
                var height = state.Nodes.Values.Where(n => n.Row == r + 1).Select(n => n.DesiredSize.Y + n.Margin.Y).Append(0).Max();
                
                var start = rowMarker;
                var center = rowMarker + height / 2;
                rowMarker = rowMarker + height + region.Config.Gutter.Rows;
                var end = rowMarker;

                rows[r] = new(start, center, end);
            }

            foreach (var obj in region.Objects)
            {
                var node = state.Nodes[obj];

                var xOrigin = (obj.Alignment.Columns ?? region.Config.Alignment.Columns) switch
                {
                    AlignmentKind.Start => columns[node.Column - 1].Start + node.Margin.Left,
                    AlignmentKind.Center => columns[node.Column - 1].Center - (node.DesiredSize.X + node.Margin.X) / 2,
                    AlignmentKind.End => columns[node.Column - 1].End - (node.DesiredSize.X + node.Margin.Right),
                };

                var yOrigin = (obj.Alignment.Rows ?? region.Config.Alignment.Rows) switch
                {
                    AlignmentKind.Start => rows[node.Row - 1].Start + node.Margin.Top,
                    AlignmentKind.Center => rows[node.Row - 1].Center - (node.DesiredSize.Y + node.Margin.Y) / 2,
                    AlignmentKind.End => rows[node.Row - 1].End - (node.DesiredSize.Y + node.Margin.Bottom),
                };

                var origin = new Point(xOrigin, yOrigin);
                var bounds = new Rect(origin, node.DesiredSize);
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
