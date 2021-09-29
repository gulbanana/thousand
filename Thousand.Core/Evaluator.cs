using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Thousand.Model;

namespace Thousand
{
    public class Evaluator
    {
        private readonly List<GenerationError> ws;
        private readonly List<GenerationError> es;
        private readonly Dictionary<string, AST.ObjectAttribute[]> classes;
        private readonly List<IR.Object> objects;
        private readonly List<IR.Edge> edges;

        public IR.Config Config { get; private set; }
        public IReadOnlyList<IR.Object> Objects => objects;
        public IReadOnlyList<IR.Edge> Edges => edges;

        public static bool TryEvaluate(IEnumerable<AST.Document> documents, List<GenerationError> ws, List<GenerationError> es, [NotNullWhen(true)] out IR.Rules? rules)
        {
            try
            {
                var evaluation = new Evaluator(ws, es, documents);
                rules = new IR.Rules(evaluation.Config, evaluation.Objects, evaluation.Edges);
                return !es.Any();
            }
            catch (Exception e)
            {
                es.Add(new(e));
                rules = null;
                return false;
            }
        }

        private Evaluator(List<GenerationError> ws, List<GenerationError> es, IEnumerable<AST.Document> documents)
        {
            this.ws = ws;
            this.es = es;

            classes = new();
            objects = new();
            edges = new();

            Config = new IR.Config();

            foreach (var doc in documents)
            {
                AddDocument(doc);
            }
        }

        private void AddDocument(AST.Document diagram)
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

                        case AST.RegionLayoutAttribute rla:
                            Config = Config with 
                            {
                                Region = Config.Region with 
                                {
                                    Layout = rla.Kind
                                }
                            };
                            break;

                        case AST.RegionMarginAttribute rma:
                            Config = Config with
                            {
                                Region = Config.Region with
                                {
                                    Margin = rma.Value
                                }
                            };
                            break;

                        case AST.RegionGutterAttribute rga:
                            Config = Config with
                            {
                                Region = Config.Region with
                                {
                                    Gutter = rga.Value
                                }
                            };
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
            var row = new int?();
            var column = new int?();
            var width = new int?();
            var height = new int?();            
            var shape = ShapeKind.RoundRect;
            var padding = 15;
            var cornerRadius = 5;
            var stroke = new Stroke();
            var fill = Colour.White;
            var region = new IR.Region();            
            var label = node.Name; // names are a separate thing, but if a node has one, it is also the default label
            var fontSize = 20;

            foreach (var attr in node.Classes.SelectMany(FindClass).Concat(node.Attributes).Concat(node.Children.Where(d => d.IsT0).Select(d => d.AsT0)))
            {
                attr.Switch(n =>
                {
                    switch (n)
                    {
                        case AST.NodeRowAttribute nra:
                            row = nra.Value;
                            break;

                        case AST.NodeColumnAttribute nca:
                            column = nca.Value;
                            break;

                        case AST.NodeWidthAttribute nwa:
                            width = nwa.Value;
                            break;

                        case AST.NodeHeightAttribute nha:
                            height = nha.Value;
                            break;

                        case AST.NodeShapeAttribute nsa:
                            shape = nsa.Kind;
                            break;

                        case AST.NodePaddingAttribute npa:
                            padding = npa.Value;
                            break;

                        case AST.NodeCornerRadiusAttribute ncra:
                            cornerRadius = ncra.Value;
                            break;
                    }
                }, r =>
                {
                    switch (r)
                    {
                        case AST.RegionFillAttribute rfa:
                            fill = rfa.Colour;
                            break;

                        case AST.RegionLayoutAttribute rla:
                            region = region with { Layout = rla.Kind };
                            break;

                        case AST.RegionMarginAttribute rma:
                            region = region with { Margin = rma.Value };
                            break;

                        case AST.RegionGutterAttribute rga:
                            region = region with { Gutter = rga.Value };
                            break;
                    }
                }, l =>
                {
                    switch (l)
                    {
                        case AST.LineStrokeColourAttribute lsca:
                            stroke = stroke with { Colour = lsca.Colour };
                            break;

                        case AST.LineStrokeWidthAttribute lswa:
                            stroke = stroke with { Width = lswa.Value };
                            break;

                        case AST.LineStrokeStyleAttribute lssa:
                            stroke = stroke with { Style = lssa.Kind };
                            break;

                        case AST.LineStrokeAttribute lsa:
                            if (lsa.Colour != null) stroke = stroke with { Colour = lsa.Colour };
                            if (lsa.Width != null) stroke = stroke with { Width = lsa.Width };
                            if (lsa.Style != null) stroke = stroke with { Style = lsa.Style.Value };
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
                objects.Add(new(node.Name, label == null ? null : new IR.Text(label, fontSize), region, row, column, width, height, shape, padding, cornerRadius, stroke, fill));
            }
        }

        private void AddEdges(AST.EdgeChain chain)
        {
            var stroke = new Stroke();
            var offsetStart = Point.Zero;
            var offsetEnd = Point.Zero;

            foreach (var attr in chain.Attributes)
            {
                attr.Switch(arrow =>
                {
                    switch (arrow)
                    {
                        case AST.ArrowOffsetStartAttribute aosa:
                            offsetStart = aosa.Offset;
                            break;

                        case AST.ArrowOffsetEndAttribute aoea:
                            offsetEnd = aoea.Offset;
                            break;

                        case AST.ArrowOffsetBothAttribute aoba:
                            offsetStart = aoba.Offset;
                            offsetEnd = aoba.Offset;
                            break;
                    }
                }, line =>
                {
                    switch (line)
                    {
                        case AST.LineStrokeColourAttribute lsa:
                            stroke = stroke with { Colour = lsa.Colour };
                            break;

                        case AST.LineStrokeWidthAttribute lwa:
                            stroke = stroke with { Width = lwa.Value };
                            break;

                        case AST.LineStrokeStyleAttribute lsa:
                            stroke = stroke with { Style = lsa.Kind };
                            break;

                        case AST.LineStrokeAttribute lsa:
                            if (lsa.Colour != null) stroke = stroke with { Colour = lsa.Colour };
                            if (lsa.Width != null) stroke = stroke with { Width = lsa.Width };
                            if (lsa.Style != null) stroke = stroke with { Style = lsa.Style.Value };
                            break;
                    }
                });
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
                        edges.Add(new(fromTarget, toTarget, offsetStart, offsetEnd, stroke));
                    }
                    else
                    {
                        edges.Add(new(toTarget, fromTarget, offsetStart, offsetEnd, stroke));
                    }
                }
            }
        }

        private AST.ObjectAttribute[] FindClass(string name)
        {
            if (!classes.ContainsKey(name))
            {
                ws.Add(new($"Object class '{name}' not defined."));
                return Array.Empty<AST.ObjectAttribute>();
            }
            else
            {
                return classes[name];
            }
        }

        private IR.Object? FindObject(string identifier)
        {
            var found = objects.Where(n => n.Name != null && n.Name.Equals(identifier, StringComparison.OrdinalIgnoreCase));
            var n = found.Count();
            if (n == 0)
            {
                ws.Add(new($"No node found with name '{identifier}'."));
                return null;
            }
            else if (n > 1)
            {
                ws.Add(new($"Multiple nodes found with name '{identifier}'."));
                return null;
            }
            else
            {
                return found.Single();
            }
        }
    }
}
