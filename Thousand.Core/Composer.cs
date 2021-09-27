using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Thousand
{
    public static class Composer
    {
        internal const int W = 150;

        public static bool TryCompose(IR.Rules ir, List<GenerationError> warnings, List<GenerationError> errors, [NotNullWhen(true)] out Layout.Diagram? diagram)
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

                var row = obj.Row ?? currentRow;
                if (obj.Label != null)
                {
                    var label = new Layout.Label(currentColumn * W - (W / 2), currentRow * W - (W / 2), obj.Label, obj.FontSize);
                    var shape = new Layout.Shape(obj.Name, new(label.X, label.Y), obj.Kind, label, obj.StrokeWidth, obj.Stroke, obj.Fill);

                    labels.Add(label);
                    shapes[obj] = shape;
                }
                else
                {
                    var shape = new Layout.Shape(obj.Name, new(currentColumn * W - (W / 2), currentRow * W - (W / 2)), obj.Kind, null, obj.StrokeWidth, obj.Stroke, obj.Fill);

                    shapes[obj] = shape;
                }

                maxRow = Math.Max(currentRow, maxRow);
                maxColumn = Math.Max(currentColumn, maxColumn);
                currentColumn++;
            }

            foreach (var edge in ir.Edges)
            {
                var from = shapes[edge.FromTarget];
                var to = shapes[edge.ToTarget];

                lines.Add(new(from, to, from.Center + edge.FromOffset, to.Center + edge.ToOffset, edge.Stroke, edge.Width));
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
