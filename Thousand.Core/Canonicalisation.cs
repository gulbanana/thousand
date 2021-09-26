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
        private readonly Dictionary<string, AST.ObjectAttribute[]> classes;
        private readonly List<IR.Object> objects;
        private readonly List<IR.Edge> edges;

        private int nextX;
        private int nextY;

        public IReadOnlyList<IR.Object> Objects => objects;
        public IReadOnlyList<IR.Edge> Edges => edges;
        public IR.Config Config { get; private set; }

        public Canonicalisation(List<string> ws, List<string> es, IEnumerable<AST.Diagram> documents)
        {
            this.ws = ws;
            this.es = es;

            classes = new();
            objects = new();
            edges = new();

            nextX = 1;
            nextY = 1;
            Config = new IR.Config(1f, Colour.White);

            foreach (var doc in documents)
            {
                AddDocument(doc);
            }
        }

        private void AddDocument(AST.Diagram diagram)
        {
            foreach (var attr in diagram.Declarations.Where(d => d.IsT0).Select(d => d.AsT0))
            {
                attr.Switch(doc =>
                {
                    switch (doc)
                    {
                        case AST.DocumentScaleAttribute dsa:
                            Config = Config with { Scale = dsa.Value };
                            break;
                    }
                }, region =>
                {
                    switch (region)
                    {
                        case AST.RegionFillAttribute rfa:
                            Config = Config with { Background = rfa.Colour };
                            break;
                    }
                });
            }

            foreach (var c in diagram.Declarations.Where(d => d.IsT1).Select(d => d.AsT1))
            {
                var attrs = c.BaseClasses
                    .SelectMany(FindClass)
                    .Concat(c.Attributes)
                    .ToArray();

                classes[c.Name] = attrs;
            }

            foreach (var node in diagram.Declarations.Where(d => d.IsT2).Select(d => d.AsT2))
            {
                AddObject(node);
            }

            foreach (var chain in diagram.Declarations.Where(d => d.IsT3).Select(d => d.AsT3))
            {
                AddEdges(chain);
            }
        }

        private void AddObject(AST.TypedObject node)
        {
            var x = nextX;
            var xSet = false;
            var y = nextY;
            var label = node.Label ?? node.Name;
            var shape = ShapeKind.RoundRect;
            var stroke = Colour.Black;
            var fill = Colour.White;
            var fontSize = 20f;
            var strokeWidth = new float?(1);

            foreach (var attr in node.Classes.SelectMany(FindClass).Concat(node.Attributes).Concat(node.Children.Where(d => d.IsT0).Select(d => d.AsT0)))
            {
                attr.Switch(n =>
                {
                    switch (n)
                    {
                        case AST.NodeShapeAttribute nsa:
                            shape = nsa.Kind;
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
                    }
                }, r =>
                {
                    switch (r)
                    {
                        case AST.RegionFillAttribute nfc:
                            fill = nfc.Colour;
                            break;
                    }
                }, l =>
                {
                    switch (l)
                    {
                        case AST.LineStrokeAttribute lsa:
                            stroke = lsa.Colour;
                            break;

                        case AST.LineWidthAttribute lwa:
                            strokeWidth = lwa.Value;
                            break;
                    }
                }, t =>
                {
                    switch (t)
                    {
                        case AST.TextLabelAttribute nla:
                            label = nla.Content;
                            break;

                        case AST.TextFontSizeAttribute nfsa:
                            fontSize = nfsa.Value;
                            break;
                    }
                });
            }

            if (node.Children.Any())
            {
                foreach (var child in node.Children.Where(d => d.IsT1).Select(d => d.AsT1))
                {
                    AddObject(child);
                }

                foreach (var chain in node.Children.Where(d => d.IsT2).Select(d => d.AsT2))
                {
                    AddEdges(chain);
                }
            }
            else
            {
                objects.Add(new(node.Name, y, x, label, shape, stroke, fill, fontSize, strokeWidth));
            }

            nextX = x + 1;
            nextY = y;
        }

        private void AddEdges(AST.EdgeChain chain)
        {
            var stroke = Colour.Black;
            var width = new float?(1);

            foreach (var attr in chain.Attributes)
            {
                switch (attr)
                {
                    case AST.LineStrokeAttribute lsa:
                        stroke = lsa.Colour;
                        break;

                    case AST.LineWidthAttribute lwa:
                        width = lwa.Value;
                        break;
                }
            }

            for (var i = 0; i < chain.Elements.Length - 1; i++)
            {
                var from = chain.Elements[i];
                var to = chain.Elements[i + 1];

                var fromTarget = FindObject(from.Target);
                var toTarget = FindObject(to.Target);

                if (fromTarget != null && toTarget != null && from.Direction.HasValue)
                {
                    if (from.Direction.Value == ArrowKind.Forward)
                    {
                        edges.Add(new(fromTarget, toTarget, stroke, width));
                    }
                    else
                    {
                        edges.Add(new(toTarget, fromTarget, stroke, width));
                    }
                }
            }
        }

        public AST.ObjectAttribute[] FindClass(string name)
        {
            if (!classes.ContainsKey(name))
            {
                ws.Add($"Object class '{name}' not defined.");
                return Array.Empty<AST.ObjectAttribute>();
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
