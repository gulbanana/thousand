using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Thousand.Model;

namespace Thousand.Compose
{
    public class Composer
    {
        public static bool TryCompose(IR.Region root, GenerationState state, [NotNullWhen(true)] out Layout.Diagram? diagram)
        {
            try
            {
                var errors = state.ErrorCount();

                var textMeasures = new Dictionary<IR.StyledText, BlockMeasurements>();
                foreach (var t in WalkLabels(root))
                {
                    textMeasures[t] = Intrinsics.TextBlock(t);
                }

                var composition = new Composer(state, root, textMeasures);
                diagram = composition.Compose();

                return state.ErrorCount() == errors;
            }
            catch (Exception ex)
            {
                state.AddError(ex);
                diagram = null;
                return false;
            }
        }

        private static IEnumerable<IR.StyledText> WalkLabels(IR.Region region)
        {
            foreach (var e in region.Entities)
            {
                switch (e)
                {
                    case IR.Object objekt:
                        if (objekt.Label != null) yield return objekt.Label;
                        foreach (var label in WalkLabels(objekt.Region))
                        {
                            yield return label;
                        }
                        break;

                    case IR.Edge edge:
                        if (edge.Label != null) yield return edge.Label;
                        break;
                }
            }
        }

        private readonly GenerationState state;
        private readonly IR.Region root;
        private readonly IReadOnlyDictionary<IR.StyledText, BlockMeasurements> textMeasures;
        private readonly Dictionary<IR.Region, LayoutState> layouts;
        private readonly Dictionary<IR.Object, Layout.Shape> globalShapes; // XXX replace this with some sort of `childShapes` in Arrange/PlaceEdge
        private readonly Stack<List<Layout.Command>> outputCommands;
        private decimal outputTransform;

        private Composer(GenerationState state, IR.Region root, IReadOnlyDictionary<IR.StyledText, BlockMeasurements> textMeasures)
        {
            this.state = state;
            this.root = root;
            this.textMeasures = textMeasures;

            layouts = new(ReferenceEqualityComparer.Instance);
            globalShapes = new(ReferenceEqualityComparer.Instance);

            outputCommands = new();
            outputTransform = 1m;
        }

        public Layout.Diagram Compose()
        {
            var rootCommands = new List<Layout.Command>();
            outputCommands.Push(rootCommands);

            // measure hierarchical regions recursively
            var rootSize = Measure(root, Point.Zero);

            // while still using the diagram entity, apply its defaults
            if (root.Config.Fill != null)
            {
                rootCommands.Add(new Layout.Shape(new Rect(rootSize), ShapeKind.Rect, 0, new Stroke(new ZeroWidth()), root.Config.Fill));
            }

            // create labels and shapes, laid out according to the measurements above and each region's settings
            Arrange(root, new Rect(rootSize));

            return new(
                (int)Math.Ceiling(rootSize.X),
                (int)Math.Ceiling(rootSize.Y),
                rootCommands
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
            var layout = new LayoutState();
            layouts[region] = layout;

            var currentRow = 1;
            var currentColumn = 1;
            var rowCount = 0;
            var columnCount = 0;

            var childObjects = region.Entities.OfType<IR.Object>();
            var childHasExplicitColumnFlow = childObjects.Any(o => o.Region.Config.GridFlow is FlowKind.Columns or FlowKind.ReverseColumns) &&
                                            !childObjects.Any(o => o.Region.Config.GridFlow is FlowKind.Rows or FlowKind.ReverseRows);

            foreach (var child in childObjects)
            {
                // measure child and apply overrides
                var (desiredSize, desiredMargin) = Measure(child);

                // anchor layout: extract positioned children from the flow and stick them to a point on the containing shape
                if (child.Anchor.HasValue)
                {
                    if (child.Row.HasValue || child.Column.HasValue)
                    {
                        state.AddError(child.Name, ErrorKind.Layout, "object {0} has both anchor and grid row/column", child.Name);
                        return Point.Zero;
                    }

                    if (child.Position != null)
                    {
                        state.AddError(child.Name, ErrorKind.Layout, "object {0} has both anchor and position", child.Name);
                        return Point.Zero;
                    }

                    var node = new AnchorLayoutNode(desiredSize, desiredMargin, child.Anchor.Value, child.Alignment.Select(k => k ?? AlignmentKind.Center));
                    layout.AllNodes[child] = node;
                    layout.AnchorNodes[child] = node;
                    continue;
                }

                // positioned layout: position at a specified point relative to the parent (which contributes to its size)
                else if (child.Position != null)
                {
                    if (child.Row.HasValue || child.Column.HasValue)
                    {
                        state.AddError(child.Name, ErrorKind.Layout, "object {0} has both position and grid row/column", child.Name);
                        return Point.Zero;
                    }

                    var node = new PositionLayoutNode(desiredSize, desiredMargin, child.Position);
                    layout.AllNodes[child] = node;
                    layout.PositionNodes[child] = node;
                    continue;
                }

                // default (grid) layout: place object in row/column tracks according to flow
                else
                {
                    var flow = region.Config.GridFlow;
                    if (flow is FlowKind.Auto)
                    {
                        if (childHasExplicitColumnFlow)
                        {
                            flow = FlowKind.Rows;
                        }
                        else
                        {
                            flow = FlowKind.Columns;
                        }
                    }

                    // grid-march: reset to manually specified cell
                    if (flow == FlowKind.Columns)
                    {
                        if (child.Row.HasValue && currentRow != child.Row.Value)
                        {
                            currentRow = child.Row.Value;
                            currentColumn = 1;
                        }
                        currentColumn = child.Column ?? currentColumn;
                    }
                    else if (flow == FlowKind.Rows)
                    {
                        if (child.Column.HasValue && currentColumn != child.Column.Value)
                        {
                            currentColumn = child.Column.Value;
                            currentRow = 1;
                        }
                        currentRow = child.Row ?? currentRow;
                    } 

                    var measurements = new GridLayoutNode(desiredSize, desiredMargin, currentRow, currentColumn);
                    layout.GridNodes[child] = measurements;
                    layout.AllNodes[child] = measurements;

                    // grid-march: update size and move to the next cell
                    rowCount = Math.Max(currentRow, rowCount);
                    columnCount = Math.Max(currentColumn, columnCount);

                    if (flow == FlowKind.Columns)
                    {
                        currentColumn++;
                        if (region.Config.GridMax != 0 && currentColumn > region.Config.GridMax)
                        {
                            currentRow++;
                            currentColumn = 1;
                        }
                    }
                    else if (flow == FlowKind.Rows)
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
            var maxWidth = layout.GridNodes.Values.Select(s => s.Size.X + s.Margin.X).Append(0).Max();
            for (var c = 0; c < columnCount; c++)
            {
                var intrinsicWidth = layout.GridNodes.Values.Where(s => s.Column == c + 1).Select(s => s.Size.X + s.Margin.X).Append(0).Max();
                var trackWidth = region.Config.Layout.Columns switch
                {
                    EqualAreaSize or EqualContentSize => maxWidth,
                    MinimumSize(var minWidth) => Math.Max(minWidth, intrinsicWidth),
                    PackedSize or _ => intrinsicWidth
                };
                layout.Columns.Add(trackWidth);
            }

            var maxHeight = layout.GridNodes.Values.Select(s => s.Size.Y + s.Margin.Y).Append(0).Max();
            for (var r = 0; r < rowCount; r++)
            {
                var intrinsicHeight = layout.GridNodes.Values.Where(s => s.Row == r + 1).Select(s => s.Size.Y + s.Margin.Y).Append(0).Max();
                var trackHeight = region.Config.Layout.Rows switch
                {
                    EqualAreaSize or EqualContentSize => maxHeight,
                    MinimumSize(var minHeight) => Math.Max(minHeight, intrinsicHeight),
                    PackedSize or _ => intrinsicHeight
                };
                layout.Rows.Add(trackHeight);
            }

            // calculate own size           
            var gridWidth = layout.Columns.Sum() + (columnCount - 1) * region.Config.Gutter.Columns;
            var gridHeight = layout.Rows.Sum() + (rowCount - 1) * region.Config.Gutter.Rows;

            var absoluteRects = layout.PositionNodes.Values.Select(pln => new Rect(pln.Origin, pln.Size)).ToList();
            var absoluteWidth = absoluteRects.Select(r => r.Right).Prepend(0).Max();
            var absoluteHeight = absoluteRects.Select(r => r.Bottom).Prepend(0).Max();

            var paddedContentSize = new Point(Math.Max(absoluteWidth, gridWidth) + region.Config.Padding.X, Math.Max(absoluteHeight, gridHeight) + region.Config.Padding.Y);
            var paddedIntrinsicSize = new Point(intrinsicSize.X + region.Config.Padding.X, intrinsicSize.Y + region.Config.Padding.Y);
            var regionSize = new Point(Math.Max(paddedIntrinsicSize.X, paddedContentSize.X), Math.Max(paddedIntrinsicSize.Y, paddedContentSize.Y));
            
            return regionSize * region.Config.Scale;
        }

        // layout objects back-to-front: shape, then intrinsic (text) content, then children
        private Dictionary<IR.Object, Rect> Arrange(IR.Object objekt, Rect bounds)
        {
            if (objekt.Shape.HasValue)
            {
                outputCommands.Peek().Add(new Layout.Shape(bounds, objekt.Shape.Value, objekt.CornerRadius, objekt.Stroke, objekt.Region.Config.Fill));
                globalShapes[objekt] = new Layout.Shape(bounds / outputTransform, objekt.Shape.Value, objekt.CornerRadius, objekt.Stroke, objekt.Region.Config.Fill);

                foreach (var kvp in Shapes.Anchors(objekt.Shape.Value, objekt.CornerRadius, bounds))
                {
                    layouts[objekt.Region].Anchors.Add(kvp.Key, kvp.Value.Location);
                }
            }

            if (objekt.Label != null && objekt.Label.Content != string.Empty)
            {
                var block = textMeasures[objekt.Label];
                var blockBox = new Rect(block.Size).CenteredAt(bounds.Center);

                // subpixel vertical positioning is not consistently supported in SVG
                var pixelBoundary = blockBox.Top * outputTransform;
                if (Math.Floor(pixelBoundary) != pixelBoundary)
                {
                    blockBox = blockBox.Move(new Point(0, 0.5m));
                }

                var lines = new List<Layout.LabelSpan>();
                foreach (var line in block.Lines)
                {
                    var lineBox = new Rect(blockBox.Origin + line.Position, line.Size);
                    lines.Add(new Layout.LabelSpan(lineBox, line.Run));
                }
                outputCommands.Peek().Add(new Layout.Label(objekt.Label.Font, blockBox, objekt.Label.Content, lines));
            }

            return Arrange(objekt.Region, bounds);
        }

        // layout region context within a given box - bounds are known to parents, so it's just a matter of dividing up the space
        private Dictionary<IR.Object, Rect> Arrange(IR.Region region, Rect bounds)
        {
            if (region.Config.Scale != 1m)
            {
                bounds /= region.Config.Scale;

                var regionCommands = new List<Layout.Command>();
                outputCommands.Peek().Add(new Layout.Transform(region.Config.Scale, regionCommands));
                outputCommands.Push(regionCommands);
                outputTransform *= region.Config.Scale;
            }

            var layout = layouts[region];

            // calculate tracks based on flow children
            var columns = new Track[layout.Columns.Count];
            var colMarker = bounds.Left + region.Config.Padding.Left;
            for (var c = 0; c < layout.Columns.Count; c++)
            {
                var width = region.Config.Layout.Columns is EqualAreaSize ? bounds.Width / layout.Columns.Count : layout.Columns[c];
                
                var start = colMarker;
                var center = colMarker + width / 2;
                colMarker = colMarker + width + region.Config.Gutter.Columns;
                var end = colMarker - region.Config.Gutter.Columns;

                columns[c] = new(start, center, end);
            }

            var rows = new Track[layout.Rows.Count];
            var rowMarker = bounds.Top + region.Config.Padding.Top;
            for (var r = 0; r < layout.Rows.Count; r++)
            {
                var height = region.Config.Layout.Rows is EqualAreaSize ? bounds.Height / layout.Rows.Count : layout.Rows[r];

                var start = rowMarker;
                var center = rowMarker + height / 2;
                rowMarker = rowMarker + height + region.Config.Gutter.Rows;
                var end = rowMarker - region.Config.Gutter.Rows;

                rows[r] = new(start, center, end);
            }

            // place objects recursively
            var childBounds = new Dictionary<IR.Object, Rect>(ReferenceEqualityComparer.Instance);
            foreach (var child in region.Entities)
            {
                switch (child)
                {
                    case IR.Object childObject:
                        foreach (var kvp in PlaceObject(region.Config, bounds, columns, rows, layout, childObject))
                        {
                            childBounds.Add(kvp.Key, kvp.Value);
                        }
                        break;

                    case IR.Edge childEdge:
                        PlaceEdge(childBounds, childEdge);
                        break;
                }
            }

            if (region.Config.Scale != 1m)
            {
                outputTransform /= region.Config.Scale;
                outputCommands.Pop();
            }

            return childBounds;
        }

        // place objects recursively
        private IEnumerable<KeyValuePair<IR.Object, Rect>> PlaceObject(IR.Config config, Rect bounds, Track[] columns, Track[] rows, LayoutState layout, IR.Object child)
        {
            var node = layout.AllNodes[child];

            var origin = node switch
            {
                // place within intersection of tracks
                GridLayoutNode gln => new Point((child.Alignment.Columns ?? config.Alignment.Columns) switch
                {
                    AlignmentKind.Start or AlignmentKind.Stretch => columns[gln.Column - 1].Start + gln.Margin.Left,
                    AlignmentKind.Center => columns[gln.Column - 1].Center - (gln.Size.X) / 2 + (gln.Margin.Left - gln.Margin.Right) / 2,
                    AlignmentKind.End => columns[gln.Column - 1].End - (gln.Size.X + gln.Margin.Right),
                }, (child.Alignment.Rows ?? config.Alignment.Rows) switch
                {
                    AlignmentKind.Start or AlignmentKind.Stretch => rows[gln.Row - 1].Start + gln.Margin.Top,
                    AlignmentKind.Center => rows[gln.Row - 1].Center - (gln.Size.Y) / 2 + (gln.Margin.Top - gln.Margin.Bottom) / 2,
                    AlignmentKind.End => rows[gln.Row - 1].End - (gln.Size.Y + gln.Margin.Bottom),
                }),

                // place *after* anchor = anchor is shape's *origin*
                AnchorLayoutNode aln when layout.Anchors.ContainsKey(aln.Anchor) => new Point((child.Alignment.Columns ?? AlignmentKind.Center) switch
                {
                    AlignmentKind.Start => (layout.Anchors[aln.Anchor] - node.Size).X,
                    AlignmentKind.Center or AlignmentKind.Stretch => (layout.Anchors[aln.Anchor] - node.Size / 2).X,
                    AlignmentKind.End => layout.Anchors[aln.Anchor].X,
                }, (child.Alignment.Rows ?? AlignmentKind.Center) switch
                {
                    AlignmentKind.Start => (layout.Anchors[aln.Anchor] - node.Size).Y,
                    AlignmentKind.Center or AlignmentKind.Stretch => (layout.Anchors[aln.Anchor] - node.Size / 2).Y,
                    AlignmentKind.End => layout.Anchors[aln.Anchor].Y,
                }),

                // place at specific loation
                PositionLayoutNode pln => bounds.Origin + pln.Origin + config.Padding.TopLeft,

                _ => Point.Zero
            };

            var size = node switch
            {
                GridLayoutNode gln => new Point((child.Alignment.Columns ?? config.Alignment.Columns) switch
                {
                    AlignmentKind.Stretch => columns[gln.Column - 1].Size,
                    _ => node.Size.X
                }, (child.Alignment.Rows ?? config.Alignment.Rows) switch
                {
                    AlignmentKind.Stretch => rows[gln.Row - 1].Size,
                    _ => node.Size.Y
                }),
                _ => node.Size
            };

            var childBounds = new Rect(origin, size);
            yield return new(child, childBounds * outputTransform);

            foreach (var kvp in Arrange(child, childBounds))
            {
                yield return kvp;
            }
        }

        // place lines, which are above the whole hierarchy - they need the margin-exclusive laid out bounds of each contained object 
        private void PlaceEdge(Dictionary<IR.Object, Rect> childBounds, IR.Edge edge)
        {
            if (edge.Label != null)
            {
                state.AddWarning(edge.To.Name, ErrorKind.Layout, "line labels have not been implemented yet");
            }

            var fromBox = childBounds[edge.From.Target] / outputTransform;
            var toBox = childBounds[edge.To.Target] / outputTransform;
            try
            {
                var (start, end) = Intrinsics.Line(fromBox.Center + edge.From.Offset, toBox.Center + edge.To.Offset, globalShapes.GetValueOrDefault(edge.From.Target), globalShapes.GetValueOrDefault(edge.To.Target));

                if (start == null)
                {
                    state.AddWarning(edge.From.Name, ErrorKind.Layout, $"failed to find point where line from {0} to {1} intersects source {0}", edge.From.Name, edge.To.Name);
                }

                if (end == null)
                {
                    state.AddWarning(edge.To.Name, ErrorKind.Layout, $"failed to find point where line from {0} to {1} intersects destination {1}", edge.From.Name, edge.To.Name);
                }

                if (start == null || end == null)
                {
                    return;
                }

                if (edge.From.Target.Shape.HasValue)
                {
                    var anchors = Shapes.Anchors(edge.From.Target.Shape.Value, edge.From.Target.CornerRadius, fromBox);

                    switch (edge.From.Anchor)
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

                    if (edge.From.Anchor is not NoAnchor && edge.To.Anchor is NoAnchor)
                    {
                        (_, end) = Intrinsics.Line(start, toBox.Center + edge.To.Offset, null, globalShapes.GetValueOrDefault(edge.To.Target));
                        if (end == null)
                        {
                            state.AddWarning(edge.From.Name, ErrorKind.Layout, $"after anchoring start, failed to find point where line from {0} to {1} intersects destination {1}", edge.From.Name, edge.To.Name);
                            return;
                        }
                    }
                }

                if (edge.To.Target.Shape.HasValue)
                {
                    var anchors = Shapes.Anchors(edge.To.Target.Shape.Value, edge.To.Target.CornerRadius, toBox);

                    switch (edge.To.Anchor)
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

                    if (edge.To.Anchor is not NoAnchor && edge.From.Anchor is NoAnchor)
                    {
                        (start, _) = Intrinsics.Line(fromBox.Center + edge.From.Offset, end, globalShapes.GetValueOrDefault(edge.From.Target), null);
                        if (start == null)
                        {
                            state.AddWarning(edge.From.Name, ErrorKind.Layout, $"after anchoring start, failed to find point where line from {0} to {1} intersects source {0}", edge.From.Name, edge.To.Name);
                            return;
                        }
                    }
                }

                outputCommands.Peek().Add(new Layout.Line(edge.Stroke, start, end, edge.From.Marker.HasValue, edge.To.Marker.HasValue));
            }
            catch (Exception ex)
            {
                state.AddWarning(ex);
                return;
            }
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
                AlignmentKind.Center or AlignmentKind.Stretch => (childAnchor - childBox / 2).X,
                AlignmentKind.End => childAnchor.X,
            }, node.Alignment.Rows switch
            {
                AlignmentKind.Start => (childAnchor - childBox).Y,
                AlignmentKind.Center or AlignmentKind.Stretch => (childAnchor - childBox / 2).Y,
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
