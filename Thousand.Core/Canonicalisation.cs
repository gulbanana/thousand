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
        private readonly List<IR.Edge> edges;

        private int nextX;
        private int nextY;

        public IReadOnlyList<IR.Object> Objects => objects;
        public IReadOnlyList<IR.Edge> Edges => edges;
        public IR.Config Config { get; private set; }

        public Canonicalisation(List<string> ws, List<string> es, IEnumerable<AST.Document> documents)
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

        private void AddDocument(AST.Document document)
        {
            foreach (var attr in document.Declarations.OfType<AST.DocumentAttribute>())
            {
                switch (attr)
                {
                    case AST.DocumentScaleAttribute dsa:
                        Config = Config with { Scale = dsa.Value };
                        break;

                    case AST.DocumentBackgroundAttribute dba:
                        Config = Config with { Background = dba.Colour };
                        break;
                }
            }

            foreach (var c in document.Declarations.OfType<AST.Class>())
            {
                var attrs = c.BaseClasses
                    .SelectMany(FindClass)
                    .Concat(c.Attributes)
                    .ToArray();

                classes[c.Name] = attrs;
            }

            foreach (var node in document.Declarations.OfType<AST.Node>())
            {
                AddObject(node);
            }

            foreach (var chain in document.Declarations.OfType<AST.Edges>())
            {
                AddEdges(chain);
            }
        }

        private void AddObject(AST.Node node)
        {
            var x = nextX;
            var xSet = false;
            var y = nextY;
            var label = node.Label ?? node.Name;
            var shape = ShapeKind.RoundRect;
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
                    AddObject(child);
                }

                foreach (var chain in node.Children.OfType<AST.Edges>())
                {
                    AddEdges(chain);
                }
            }
            else
            {
                objects.Add(new(node.Name, y, x, label, shape, stroke, fill, fontSize));
            }

            nextX = x + 1;
            nextY = y;
        }

        private void AddEdges(AST.Edges chain)
        {
            var stroke = Colour.Black;
            var width = new float?();

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
