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

        public static bool TryCompose(AST.Document document, [NotNullWhen(true)] out Layout.Diagram? diagram, out IReadOnlyList<string> warnings, out IReadOnlyList<string> errors)
        {
            var ws = new List<string>();
            var es = new List<string>();

            // pass 1: canonicalise AST elements, arranging the nodes on a grid 
            var labelledShapes = new List<LabelledShape>();

            var nextX = 1;
            var nextY = 1;
            foreach (var node in document.Declarations.OfType<AST.Node>())
            {
                var x = nextX;
                var y = nextY;
                var label = node.Label;
                var shape = ShapeKind.Square;
                var stroke = Colour.Black;
                var fill = Colour.White;

                foreach (var attr in node.Attributes)
                {
                    switch (attr)
                    {
                        case AST.NodeLabelAttribute nla:
                            label = nla.Content;
                            break;

                        case AST.NodeShapeAttribute nsa:
                            shape = nsa.Kind;
                            break;

                        case AST.NodeStrokeAttribute nsc:
                            stroke = nsc.Colour;
                            break;

                        case AST.NodeFillAttribute nfc:
                            fill = nfc.Colour;
                            break;

                        case AST.NodeRowAttribute nra:
                            y = nra.Value;
                            break;

                        case AST.NodeColumnAttribute nca:
                            x = nca.Value;
                            break;
                    }
                }

                labelledShapes.Add(new(y, x, node.Label, shape, stroke, fill));

                nextX = x + 1;
                nextY = y;
            }

            // pass 2: 
            var shapes = new List<Layout.Shape>();
            var labels = new List<Layout.Label>();
            var lines = new List<Layout.Line>();

            foreach (var node in labelledShapes)
            {
                var label = new Layout.Label(node.Column * W - (W/2), node.Row * W - (W / 2), node.Label);
                var shape = new Layout.Shape(label.X, label.Y, node.Kind, label, node.Stroke, node.Fill);

                labels.Add(label);
                shapes.Add(shape);

                nextX += W;
            }

            Layout.Shape? find(string label)
            {
                var found = shapes.Where(n => n.Fit.Content.Equals(label, StringComparison.OrdinalIgnoreCase));
                var n = found.Count();
                if (n == 0)
                {
                    ws.Add($"No node found matching label '{label}'.");
                    return null;
                }
                else if (n > 1)
                {
                    ws.Add($"Multiple nodes found matching label '{label}'.");
                    return null;
                }
                else
                {
                    return found.Single();
                }
            }

            foreach (var edge in document.Declarations.OfType<AST.Edge>())
            {
                var nFrom = find(edge.From);
                var nTo = find(edge.To);
                if (nFrom == null || nTo == null) continue;

                var colour = Colour.Black;

                foreach (var attr in edge.Attributes)
                {
                    switch (attr)
                    {
                        case AST.EdgeStrokeAttribute esa:
                            colour = esa.Colour;
                            break;
                    }
                }

                lines.Add(new(nFrom, nTo, colour));
            }


            warnings = ws;
            errors = es;
            diagram = new(
                labelledShapes.Select(s => s.Column).Max() * W,
                labelledShapes.Select(s => s.Row).Max() * W, 
                shapes, 
                labels, 
                lines
            );

            return !es.Any();
        }
    }
}
