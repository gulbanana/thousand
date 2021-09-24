using System;
using System.Collections.Generic;
using System.Linq;
using Thousand.Model;

namespace Thousand
{
    internal class Canonicalisation
    {
        private readonly List<string> ws;
        private readonly List<string> es;
        private readonly Dictionary<string, AST.NodeAttribute[]> classes;
        private readonly List<IR.Object> objects;
        public IReadOnlyList<IR.Object> Objects => objects;
        private int nextX;
        private int nextY;

        public Canonicalisation(List<string> ws, List<string> es, AST.Document document)
        {
            this.ws = ws;
            this.es = es;

            classes = new Dictionary<string, AST.NodeAttribute[]>(StringComparer.OrdinalIgnoreCase)
            {
                { "object", Array.Empty<AST.NodeAttribute>() }
            };

            foreach (var c in document.Declarations.OfType<AST.Class>())
            {
                var attrs = c.BaseClasses
                    .SelectMany(FindClass)
                    .Concat(c.Attributes)
                    .ToArray();

                classes[c.Name] = attrs;
            }

            objects = new();

            nextX = 1;
            nextY = 1;
            foreach (var node in document.Declarations.OfType<AST.Node>())
            {
                MakeObject(node);
            }
        }

        private void MakeObject(AST.Node node)
        {
            var x = nextX;
            var xSet = false;
            var y = nextY;
            var label = node.Label ?? node.Name;
            var shape = ShapeKind.Square;
            var stroke = Colour.Black;
            var fill = Colour.White;
            var fontSize = 20f;

            foreach (var attr in node.Classes.SelectMany(FindClass).Concat(node.Attributes))
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

            if (node.Children.Any())
            {
                foreach (var child in node.Children.OfType<AST.Node>())
                {
                    MakeObject(child);
                }
            }
            else
            {
                objects.Add(new(node.Name, y, x, label, shape, stroke, fill, fontSize));
            }

            nextX = x + 1;
            nextY = y;
        }

        public AST.NodeAttribute[] FindClass(string name)
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
        }

        public IR.Object? FindObject(string identifierOrLabel)
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
    }
}
