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
                var textMeasures = new Dictionary<IR.StyledText, BlockMeasurements>();
                foreach (var t in ir.WalkObjects().Select(o => o.Label).WhereNotNull())
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
        private readonly IReadOnlyDictionary<IR.StyledText, BlockMeasurements> textMeasures;
        private readonly Dictionary<IR.Region, LayoutState> layouts;
        private readonly Dictionary<IR.Object, Layout.Shape> outputShapes;
        private readonly List<Layout.LabelBlock> outputLabels;
        private readonly List<Layout.Line> outputLines;

        private Composer(List<GenerationError> warnings, List<GenerationError> errors, IR.Root root, IReadOnlyDictionary<IR.StyledText, BlockMeasurements> textMeasures)
        {
            ws = warnings;
            es = errors;

            this.root = root;
            this.textMeasures = textMeasures;

            layouts = new(ReferenceEqualityComparer.Instance);

            outputShapes = new(ReferenceEqualityComparer.Instance);
            outputLabels = new();
            outputLines = new();            
        }

        // XXX consider positioning the label in the region rather than overlaid 
        public Layout.Diagram Compose()
        {
            // measure hierarchical regions recursively
            var rootSize = Measure(root.Region, Point.Zero);

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

                    if (start == null)
                    {
                        ws.Add(new(edge.FromName.Location, ErrorKind.Layout, $"failed to find point where line from `{edge.FromName.Text}` to `{edge.ToName.Text}` intersects `{edge.FromName.Text}`"));
                    }

                    if (end == null)
                    {
                        ws.Add(new(edge.ToName.Location, ErrorKind.Layout, $"failed to find point where line from `{edge.FromName.Text}` to `{edge.ToName.Text}` intersects `{edge.ToName.Text}`"));
                    }

                    if (start == null || end == null)
                    {
                        continue;
                    }

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
                            if (end == null)
                            {
                                ws.Add(new(edge.FromName.Location, ErrorKind.Layout, $"after anchoring start, failed to find point where line from `{edge.FromName.Text}` to `{edge.ToName.Text}` intersects `{edge.ToName.Text}`"));
                                continue;
                            }
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
                            if (start == null)
                            {
                                ws.Add(new(edge.FromName.Location, ErrorKind.Layout, $"after anchoring end, failed to find point where line from `{edge.FromName.Text}` to `{edge.ToName.Text}` intersects `{edge.FromName.Text}`"));
                                continue;
                            }
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

            var outerSize = rootSize; //.DesiredSize + rootSize.DesiredMargin;
            return new(
                (int)Math.Ceiling(outerSize.X),
                (int)Math.Ceiling(outerSize.Y),
                root.Scale,
                root.Region.Config.Fill,
                outputShapes.Values.ToList(), 
                outputLabels, 
                outputLines
            );
        }

        // each object can have 0 or more of the following:
        // a) child content (other objects)
        // b) intrinsic content (a label)
        // c) a shape stroked around (and filled behind) the larger of (a) and (b)
        // all of these, plus padding, contribute to a object's bounding box size
        private RegionMeasurements Measure(IR.Object objekt)
        {
            var desiredSize = Measure(
                objekt.Region,
                objekt.Label == null ? Point.Zero : textMeasures[objekt.Label].Size
            );

            if (objekt.MinWidth.HasValue)
            {
                desiredSize = desiredSize with { X = Math.Max(objekt.MinWidth.Value, desiredSize.X) };
            }

            if (objekt.MinHeight.HasValue)
            {
                desiredSize = desiredSize with { Y = Math.Max(objekt.MinHeight.Value, desiredSize.Y) };
            }

            if (objekt.Shape is ShapeKind.Square or ShapeKind.Roundsquare or ShapeKind.Circle or ShapeKind.Diamond)
            {
                var longestSide = Math.Max(desiredSize.X, desiredSize.Y);
                desiredSize = new Point(longestSide, longestSide);
            }

            var desiredMargin = layouts[objekt.Region].AnchorNodes.Values
                .Select(aln => GetAnchorBorder(aln, objekt, desiredSize))
                .Aggregate(objekt.Margin, (b1, b2) => b1.Combine(b2));

            return new RegionMeasurements(desiredSize, desiredMargin);
        }

        // a region (which can be the children of some other object, or the root) has
        // a size determined by the object bounds, their margins and the region's gutter
        private Point Measure(IR.Region region, Point intrinsicSize)
        {
            var state = new LayoutState();
            layouts[region] = state;

            var currentRow = 1;
            var currentColumn = 1;
            var rowCount = 0;
            var columnCount = 0;

            foreach (var child in region.Objects)
            {
                // measure child and apply overrides
                var (desiredSize, desiredMargin) = Measure(child);

                // anchor layout: extract positioned children from the flow and stick them to a point on the containing shape
                if (child.Anchor.HasValue)
                {
                    if (child.Row.HasValue || child.Column.HasValue)
                    {
                        es.Add(new(child.Name.Location, ErrorKind.Layout, $"object `{child.Name.Text}` has both anchor and grid row/column"));
                        return Point.Zero;
                    }

                    var node = new AnchorLayoutNode(desiredSize, desiredMargin, child.Anchor.Value, child.Alignment.Select(k => k ?? AlignmentKind.Center));
                    state.AllNodes[child] = node;
                    state.AnchorNodes[child] = node;
                    continue;
                }

                // default (grid) layout: place object in row/column tracks according to flow
                else
                {
                    // grid-march: reset to manually specified cell
                    if (region.Config.GridFlow == FlowKind.Columns)
                    {
                        if (child.Row.HasValue && currentRow != child.Row.Value)
                        {
                            currentRow = child.Row.Value;
                            currentColumn = 1;
                        }
                        currentColumn = child.Column ?? currentColumn;
                    }
                    else if (region.Config.GridFlow == FlowKind.Rows)
                    {
                        if (child.Column.HasValue && currentColumn != child.Column.Value)
                        {
                            currentColumn = child.Column.Value;
                            currentRow = 1;
                        }
                        currentRow = child.Row ?? currentRow;
                    } 

                    var measurements = new GridLayoutNode(desiredSize, desiredMargin, currentRow, currentColumn);
                    state.GridNodes[child] = measurements;
                    state.AllNodes[child] = measurements;

                    // grid-march: update size and move to the next cell
                    rowCount = Math.Max(currentRow, rowCount);
                    columnCount = Math.Max(currentColumn, columnCount);

                    if (region.Config.GridFlow == FlowKind.Columns)
                    {
                        currentColumn++;
                        if (region.Config.GridMax != 0 && currentColumn > region.Config.GridMax)
                        {
                            currentRow++;
                            currentColumn = 1;
                        }
                    }
                    else if (region.Config.GridFlow == FlowKind.Rows)
                    {
                        currentRow++;
                        if (region.Config.GridMax != 0 && currentRow > region.Config.GridMax)
                        {
                            currentColumn++;
                            currentRow = 1;
                        }
                    }
                }
            }

            // calculate track sizes
            var maxWidth = state.GridNodes.Values.Select(s => s.Size.X + s.Margin.X).Append(0).Max();
            for (var c = 0; c < columnCount; c++)
            {
                var intrinsicWidth = state.GridNodes.Values.Where(s => s.Column == c + 1).Select(s => s.Size.X + s.Margin.X).Append(0).Max();
                var trackWidth = region.Config.Layout.Columns switch
                {
                    EqualSize => maxWidth,
                    MinimumSize(var minWidth) => Math.Max(minWidth, intrinsicWidth),
                    PackedSize or _ => intrinsicWidth
                };
                state.Columns.Add(trackWidth);
            }

            var maxHeight = state.GridNodes.Values.Select(s => s.Size.Y + s.Margin.Y).Append(0).Max();
            for (var r = 0; r < rowCount; r++)
            {
                var intrinsicHeight = state.GridNodes.Values.Where(s => s.Row == r + 1).Select(s => s.Size.Y + s.Margin.Y).Append(0).Max();
                var trackHeight = region.Config.Layout.Rows switch
                {
                    EqualSize => maxHeight,
                    MinimumSize(var minHeight) => Math.Max(minHeight, intrinsicHeight),
                    PackedSize or _ => intrinsicHeight
                };
                state.Rows.Add(trackHeight);
            }

            // calculate own size
            var intrinsicPadding = intrinsicSize == Point.Zero ? new Border(0) : region.Config.Padding;
            var paddedIntrinsicSize = new Point(intrinsicSize.X + intrinsicPadding.X, intrinsicSize.Y + intrinsicPadding.Y);
            var regionPadding = rowCount + columnCount == 0 ? new Border(0) : region.Config.Padding;

            var contentWidth = state.Columns.Sum() + (columnCount - 1) * region.Config.Gutter.Columns + regionPadding.X;
            var contentHeight = state.Rows.Sum() + (rowCount - 1) * region.Config.Gutter.Rows + regionPadding.Y;

            var regionSize = new Point(contentWidth, contentHeight);
            var contentSize = new Point(Math.Max(paddedIntrinsicSize.X, regionSize.X), Math.Max(paddedIntrinsicSize.Y, regionSize.Y));
            
            return contentSize;
        }

        // lays out a region's contents at a given point - size is known to parents, so it's just a matter of dividing up the space
        // paint back-to-front: shape, then intrinsic content, then c hildren
        private Dictionary<IR.Object, Rect> Arrange(IR.Region region, Point location)
        {
            var boxes = new Dictionary<IR.Object, Rect>(ReferenceEqualityComparer.Instance);
            var state = layouts[region];

            // calculate tracks based on flow children
            var columns = new Track[state.Columns.Count];
            var colMarker = location.X + region.Config.Padding.Left;
            for (var c = 0; c < state.Columns.Count; c++)
            {
                var width = state.Columns[c];
                
                var start = colMarker;
                var center = colMarker + width / 2;
                colMarker = colMarker + width + region.Config.Gutter.Columns;
                var end = colMarker;

                columns[c] = new(start, center, end);
            }

            var rows = new Track[state.Rows.Count];
            var rowMarker = location.Y + region.Config.Padding.Top;
            for (var r = 0; r < state.Rows.Count; r++)
            {
                var height = state.Rows[r];
                
                var start = rowMarker;
                var center = rowMarker + height / 2;
                rowMarker = rowMarker + height + region.Config.Gutter.Rows;
                var end = rowMarker;

                rows[r] = new(start, center, end);
            }

            // arrange each child in the tracks recursively
            foreach (var child in region.Objects)
            {
                var node = state.AllNodes[child];

                var origin = node switch
                {
                    GridLayoutNode gln => new Point((child.Alignment.Columns ?? region.Config.Alignment.Columns) switch
                    {
                        AlignmentKind.Start => columns[gln.Column - 1].Start + gln.Margin.Left,
                        AlignmentKind.Center => columns[gln.Column - 1].Center - (gln.Size.X) / 2,
                        AlignmentKind.End => columns[gln.Column - 1].End - (gln.Size.X + gln.Margin.Right),
                    }, (child.Alignment.Rows ?? region.Config.Alignment.Rows) switch
                    {
                        AlignmentKind.Start => rows[gln.Row - 1].Start + gln.Margin.Top,
                        AlignmentKind.Center => rows[gln.Row - 1].Center - (gln.Size.Y) / 2,
                        AlignmentKind.End => rows[gln.Row - 1].End - (gln.Size.Y + gln.Margin.Bottom),
                    }),

                    // *after* anchor = anchor is shape's *origin*
                    AnchorLayoutNode aln when state.Anchors.ContainsKey(aln.Anchor) => new Point((child.Alignment.Columns ?? AlignmentKind.Center) switch
                    {
                        AlignmentKind.Start => (state.Anchors[aln.Anchor] - node.Size).X,
                        AlignmentKind.Center => (state.Anchors[aln.Anchor] - node.Size / 2).X,
                        AlignmentKind.End => state.Anchors[aln.Anchor].X,
                    }, (child.Alignment.Rows ?? AlignmentKind.Center) switch
                    {
                        AlignmentKind.Start => (state.Anchors[aln.Anchor] - node.Size).Y,
                        AlignmentKind.Center => (state.Anchors[aln.Anchor] - node.Size / 2).Y,
                        AlignmentKind.End => state.Anchors[aln.Anchor].Y,
                    }),
                    
                    _ => Point.Zero
                };

                var bounds = new Rect(origin, node.Size);
                boxes[child] = bounds;

                // paint shape and determine its anchors for its own children to use
                if (child.Shape.HasValue)
                {
                    var shape = new Layout.Shape(bounds, child.Shape.Value, child.CornerRadius, child.Stroke, child.Region.Config.Fill);
                    outputShapes[child] = shape;

                    foreach (var kvp in Shapes.Anchors(child.Shape.Value, child.CornerRadius, bounds))
                    {
                        layouts[child.Region].Anchors.Add(kvp.Key, kvp.Value.Location);
                    }
                }

                // paint intrinsic (text) content 
                if (child.Label != null && child.Label.Content != string.Empty)
                {
                    var block = textMeasures[child.Label];
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
                    var label = new Layout.LabelBlock(child.Label.Font, blockBox, child.Label.Content, lines);
                    outputLabels.Add(label);
                }

                if (child.Region.Objects.Any())
                {
                    foreach (var kvp in Arrange(child.Region, bounds.Origin))
                    {
                        boxes.Add(kvp.Key, kvp.Value);
                    }
                }
            }

            return boxes;
        }

        private static Border GetAnchorBorder(AnchorLayoutNode node, IR.Object parent, Point parentSize)
        {
            if (!parent.Shape.HasValue)
            {
                return Border.Zero;
            }

            var parentBounds = new Rect(parentSize);
            var parentAnchors = Shapes.Anchors(parent.Shape.Value, parent.CornerRadius, parentBounds);

            if (!parentAnchors.ContainsKey(node.Anchor))
            {
                return Border.Zero;
            }

            var childBox = node.Size;
            var childAnchor = parentAnchors[node.Anchor].Location;
            var childOrigin = new Point(node.Alignment.Columns switch
            {
                AlignmentKind.Start => (childAnchor - childBox).X,
                AlignmentKind.Center => (childAnchor - childBox / 2).X,
                AlignmentKind.End => childAnchor.X,
            }, node.Alignment.Rows switch
            {
                AlignmentKind.Start => (childAnchor - childBox).Y,
                AlignmentKind.Center => (childAnchor - childBox / 2).Y,
                AlignmentKind.End => childAnchor.Y,
            });
            var childBounds = new Rect(childOrigin, childBox) + node.Margin;

            return new Border(
                Math.Max(0, parentBounds.Left - childBounds.Left),
                Math.Max(0, parentBounds.Top - childBounds.Top),
                Math.Max(0, childBounds.Right - parentBounds.Right),
                Math.Max(0, childBounds.Bottom - parentBounds.Bottom)
            );
        }
    }
}
