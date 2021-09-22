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

        private record Object(string? Name, int Row, int Column, string? Label, ShapeKind Kind, Colour Stroke, Colour Fill, float FontSize);
        private record Edge(Object FromTarget, Object ToTarget, Colour Stroke);

        public static bool TryCompose(AST.Document document, [NotNullWhen(true)] out Layout.Diagram? diagram, out GenerationError[] warnings, out GenerationError[] errors)
        {
            var ws = new List<string>();
            var es = new List<string>();

            // pass 0: register classes
            var classes = new Dictionary<string, AST.NodeAttribute[]>(StringComparer.OrdinalIgnoreCase)
            {
                { "object", Array.Empty<AST.NodeAttribute>() }
            };

            AST.NodeAttribute[] findClass(string name)
            {
                if (!classes.ContainsKey(name))
                {
                    ws.Add($"Object class '{name}' not defined.");
                    return Array.Empty<AST.NodeAttribute>();
                }
                else
                {
                    return classes[name];
                }
            };

            foreach (var c in document.Declarations.OfType<AST.Class>())
            {
                var attrs = c.BaseClasses
                    .SelectMany(findClass)
                    .Concat(c.Attributes)
                    .ToArray();

                classes[c.Name] = attrs;
            }

            // pass 1: canonicalise AST elements, arranging the nodes on a grid and extracting edges from chains
            var objects = new List<Object>();

            var nextX = 1;
            var nextY = 1;
            foreach (var node in document.Declarations.OfType<AST.Node>())
            {
                var x = nextX;
                var xSet = false;
                var y = nextY;
                var label = node.Label ?? node.Name;
                var shape = ShapeKind.Square;
                var stroke = Colour.Black;
                var fill = Colour.White;
                var fontSize = 20f;

                foreach (var attr in node.Classes.SelectMany(findClass).Concat(node.Attributes))
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
                            if (y != nra.Value)
                            {
                                y = nra.Value;
                                if (!xSet)
                                {
                                    x = 1;
                                }
                            }
                            break;

                        case AST.NodeColumnAttribute nca:
                            x = nca.Value;
                            xSet = true;
                            break;

                        case AST.NodeFontSizeAttribute nfsa:
                            fontSize = nfsa.Value;
                            break;
                    }
                }

                objects.Add(new(node.Name, y, x, label, shape, stroke, fill, fontSize));

                nextX = x + 1;
                nextY = y;
            }

            var edges = new List<Edge>();

            Object? find(string identifierOrLabel)
            {
                var found = objects.Where(n => (n.Name != null && n.Name.Equals(identifierOrLabel, StringComparison.OrdinalIgnoreCase)) ||
                                              (n.Label != null && n.Label.Equals(identifierOrLabel, StringComparison.OrdinalIgnoreCase)));
                var n = found.Count();
                if (n == 0)
                {
                    ws.Add($"No node found with name or label '{identifierOrLabel}'.");
                    return null;
                }
                else if (n > 1)
                {
                    ws.Add($"Multiple nodes found with name or label '{identifierOrLabel}'.");
                    return null;
                }
                else
                {
                    return found.Single();
                }
            }

            foreach (var chain in document.Declarations.OfType<AST.Edges>())
            {
                var stroke = Colour.Black;

                foreach (var attr in chain.Attributes)
                {
                    switch (attr)
                    {
                        case AST.EdgeStrokeAttribute esa:
                            stroke = esa.Colour;
                            break;
                    }
                }

                for (var i = 0; i < chain.Elements.Length - 1; i++)
                {
                    var from = chain.Elements[i];
                    var to = chain.Elements[i+1];

                    var fromTarget = find(from.Target);
                    var toTarget = find(to.Target);

                    if (fromTarget != null && toTarget != null && from.Direction.HasValue)
                    {
                        if (from.Direction.Value == ArrowKind.Forward)
                        {
                            edges.Add(new(fromTarget, toTarget, stroke));
                        }
                        else
                        {
                            edges.Add(new(toTarget, fromTarget, stroke));
                        }
                    }                    
                }
            }

            // pass 2: create drawables from the canonicalised AST
            var shapes = new Dictionary<Object, Layout.Shape>();
            var labels = new List<Layout.Label>();
            var lines = new List<Layout.Line>();

            foreach (var obj in objects)
            {
                if (obj.Label != null)
                {
                    var label = new Layout.Label(obj.Column * W - (W / 2), obj.Row * W - (W / 2), obj.Label, obj.FontSize);
                    var shape = new Layout.Shape(obj.Name, label.X, label.Y, obj.Kind, label, obj.Stroke, obj.Fill);

                    labels.Add(label);
                    shapes.Add(obj, shape);
                }
                else
                {
                    var shape = new Layout.Shape(obj.Name, obj.Column * W - (W / 2), obj.Row * W - (W / 2), obj.Kind, null, obj.Stroke, obj.Fill);

                    shapes.Add(obj, shape);
                }

                nextX += W;
            }

            foreach (var edge in edges)
            {
                var from = shapes[edge.FromTarget];
                var to = shapes[edge.ToTarget];

                lines.Add(new(from, to, edge.Stroke));
            }

            // pass 3: apply doc-level attributes
            var scale = 1f;
            var background = Colour.White;            

            foreach (var attr in document.Declarations.OfType<AST.DocumentAttribute>())
            {
                switch (attr)
                {
                    case AST.DocumentScaleAttribute dsa:
                        scale = dsa.Value;
                        break;

                    case AST.DocumentBackgroundAttribute dba:
                        background = dba.Colour;
                        break;
                }
            }

            warnings = ws.Select(w => new GenerationError(w)).ToArray();
            errors = es.Select(w => new GenerationError(w)).ToArray();
            diagram = new(
                objects.Select(s => s.Column).Append(0).Max() * W,
                objects.Select(s => s.Row).Append(0).Max() * W, 
                scale,
                background,
                shapes.Values.ToList(), 
                labels, 
                lines
            );

            return !es.Any();
        }
    }
}
