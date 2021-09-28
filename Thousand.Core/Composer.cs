using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Thousand.Model;

namespace Thousand
{
    public static class Composer
    {
        internal const int W = 150;

        public static bool TryCompose(IR.Rules ir, IReadOnlyDictionary<string, Point> measures, List<GenerationError> warnings, List<GenerationError> errors, [NotNullWhen(true)] out Layout.Diagram? diagram)
        {
            var shapes = new Dictionary<IR.Object, Layout.Shape>();
            var labels = new List<Layout.Label>();
            var lines = new List<Layout.Line>();

            var currentRow = 1;
            var maxRow = 1;
            var currentColumn = 1;
            var maxColumn = 1;
            foreach (var obj in ir.Objects)
            {
                if (obj.Row.HasValue)
                {
                    currentRow = obj.Row.Value;
                    currentColumn = 1;
                }

                if (obj.Column.HasValue)
                {
                    currentColumn = obj.Column.Value;
                }

                var center = new Point(currentColumn * W - (W / 2), currentRow * W - (W / 2));
                Rect box;
                if (obj.Text != null && obj.Text.Label != string.Empty)
                {
                    var textBox = new Rect(measures[obj.Text.Label]).CenteredAt(center);
                    var label = new Layout.Label(textBox, obj.Text.Label, obj.Text.FontSize);
                    labels.Add(label);

                    box = new Rect(measures[obj.Text.Label]).CenteredAt(center).Pad(obj.Padding);
                }
                else
                {
                    box = new Rect(center, Point.Zero);
                }

                if (obj.Width.HasValue || obj.Height.HasValue)
                {
                    box = box.Grow(obj.Width ?? box.Width, obj.Height ?? box.Height);
                }

                if (obj.Kind is ShapeKind.Square or ShapeKind.RoundSquare or ShapeKind.Circle) box = box.Square();
                var shape = new Layout.Shape(obj.Name, obj.Kind, box, obj.CornerRadius, obj.Stroke.Width, obj.Stroke.Colour, obj.Fill);
                shapes[obj] = shape;

                maxRow = Math.Max(currentRow, maxRow);
                maxColumn = Math.Max(currentColumn, maxColumn);
                currentColumn++;
            }

            foreach (var edge in ir.Edges)
            {
                var from = shapes[edge.FromTarget];
                var to = shapes[edge.ToTarget];

                lines.Add(new(from, to, from.Bounds.Center() + edge.FromOffset, to.Bounds.Center() + edge.ToOffset, edge.Stroke, edge.Width));
            }

            diagram = new(
                maxColumn * W,
                maxRow * W, 
                ir.Config.Scale,
                ir.Config.Background,
                shapes.Values.ToList(), 
                labels, 
                lines
            );

            return !errors.Any();
        }
    }
}
