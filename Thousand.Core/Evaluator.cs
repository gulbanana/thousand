using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Thousand.Model;

namespace Thousand
{
    public class Evaluator
    {
        public static bool TryEvaluate(IEnumerable<AST.Document> documents, List<GenerationError> warnings, List<GenerationError> errors, [NotNullWhen(true)] out IR.Rules? rules)
        {
            try
            {
                var evaluation = new Evaluator(warnings, errors);
                foreach (var doc in documents)
                {
                    evaluation.AddDocument(doc);
                }
                rules = new IR.Rules(evaluation.Config, evaluation.Objects, evaluation.Edges);

                return !errors.Any();
            }
            catch (Exception e)
            {
                errors.Add(new(e));
                rules = null;
                return false;
            }
        }

        private readonly List<GenerationError> ws;
        private readonly List<GenerationError> es;
        private readonly Dictionary<string, AST.ObjectAttribute[]> objectClasses;
        private readonly Dictionary<string, AST.LineAttribute[]> lineClasses;
        private readonly Dictionary<string, IR.Object> objects;
        private readonly List<IR.Edge> lines;

        public IR.Config Config { get; private set; }
        public IReadOnlyList<IR.Object> Objects => objects.Values.ToList();
        public IReadOnlyList<IR.Edge> Edges => lines;

        private Evaluator(List<GenerationError> warnings, List<GenerationError> errors)
        {
            ws = warnings;
            es = errors;

            objectClasses = new(StringComparer.OrdinalIgnoreCase);
            lineClasses = new(StringComparer.OrdinalIgnoreCase);
            objects = new(StringComparer.OrdinalIgnoreCase);
            lines = new();

            Config = new IR.Config();
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
                if (c is AST.ObjectClass || c is AST.ObjectOrLineClass && c.BaseClasses.All(b => objectClasses.ContainsKey(b)))
                {
                    var localAttrs = c switch
                    {
                        AST.ObjectClass oc => oc.Attributes,
                        AST.ObjectOrLineClass olc => olc.Attributes.Select(ec => new AST.ObjectAttribute(ec)),
                        _ => Enumerable.Empty<AST.ObjectAttribute>()
                    };

                    var allAttrs = c.BaseClasses
                        .SelectMany(FindObjectClass)
                        .Concat(localAttrs)
                        .ToArray();

                    objectClasses[c.Name] = allAttrs;
                }

                if (c is AST.LineClass || c is AST.ObjectOrLineClass && c.BaseClasses.All(b => lineClasses.ContainsKey(b)))
                {
                    var localAttrs = c switch
                    {
                        AST.LineClass lc => lc.Attributes,
                        AST.ObjectOrLineClass olc => olc.Attributes.Select(ec => new AST.LineAttribute(ec)),
                        _ => Enumerable.Empty<AST.LineAttribute>()
                    };

                    var allAttrs = c.BaseClasses
                        .SelectMany(FindLineClass)
                        .Concat(localAttrs)
                        .ToArray();

                    lineClasses[c.Name] = allAttrs;
                }
            }

            foreach (var node in diagram.Declarations.Where(d => d.IsT2).Select(d => d.AsT2))
            {
                AddObject(node);
            }

            foreach (var chain in diagram.Declarations.Where(d => d.IsT3).Select(d => d.AsT3))
            {
                AddLine(chain);
            }
        }

        private void AddObject(AST.TypedObject node)
        {
            var name = node.Name ?? Guid.NewGuid().ToString();

            if (objects.ContainsKey(name))
            {
                es.Add(new GenerationError($"Duplicate object name '{name}'."));
                return;
            }

            var row = new int?();
            var column = new int?();
            var width = new int?();
            var height = new int?();            
            var shape = new ShapeKind?(ShapeKind.RoundRectangle);
            var padding = 15;
            var cornerRadius = 5;
            var stroke = new Stroke();
            var fill = Colour.White;
            var region = new IR.Region();            
            var label = node.Name; // names are a separate thing, but if a node has one, it is also the default label
            var fontSize = 20;

            foreach (var attr in node.Classes.SelectMany(FindObjectClass).Concat(node.Attributes).Concat(node.Children.Where(d => d.IsT0).Select(d => d.AsT0)))
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
                        case AST.StrokeColourAttribute lsca:
                            stroke = stroke with { Colour = lsca.Colour };
                            break;

                        case AST.StrokeWidthAttribute lswa:
                            stroke = stroke with { Width = lswa.Value };
                            break;

                        case AST.StrokeStyleAttribute lssa:
                            stroke = stroke with { Style = lssa.Kind };
                            break;

                        case AST.StrokeShorthandAttribute lsa:
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
                    AddLine(chain);
                }
            }
            else
            {
                objects.Add(name, new(label == null ? null : new IR.Text(label, fontSize), region, row, column, width, height, shape, padding, cornerRadius, stroke, fill));
            }
        }

        private void AddLine(AST.TypedLine line)
        {
            var stroke = new Stroke();
            var offsetStart = Point.Zero;
            var offsetEnd = Point.Zero;

            foreach (var attr in line.Classes.SelectMany(FindLineClass).Concat(line.Attributes))
            {
                attr.Switch(arrow =>
                {
                    switch (arrow)
                    {
                        case AST.ArrowOffsetStartXAttribute aosa:
                            offsetStart = offsetStart with { X = aosa.Offset };
                            break;

                        case AST.ArrowOffsetStartYAttribute aosa:
                            offsetStart = offsetStart with { Y = aosa.Offset };
                            break;

                        case AST.ArrowOffsetEndXAttribute aoea:
                            offsetEnd = offsetEnd with { X = aoea.Offset };
                            break;

                        case AST.ArrowOffsetEndYAttribute aoea:
                            offsetEnd = offsetEnd with { Y = aoea.Offset };
                            break;

                        case AST.ArrowOffsetXAttribute aoa:
                            offsetStart = offsetStart with { X = aoa.Offset };
                            offsetEnd = offsetEnd with { X = aoa.Offset };
                            break;

                        case AST.ArrowOffsetYAttribute aoa:
                            offsetStart = offsetStart with { Y = aoa.Offset };
                            offsetEnd = offsetEnd with { Y = aoa.Offset };
                            break;
                    }
                }, line =>
                {
                    switch (line)
                    {
                        case AST.StrokeColourAttribute lsa:
                            stroke = stroke with { Colour = lsa.Colour };
                            break;

                        case AST.StrokeWidthAttribute lwa:
                            stroke = stroke with { Width = lwa.Value };
                            break;

                        case AST.StrokeStyleAttribute lsa:
                            stroke = stroke with { Style = lsa.Kind };
                            break;

                        case AST.StrokeShorthandAttribute lsa:
                            if (lsa.Colour != null) stroke = stroke with { Colour = lsa.Colour };
                            if (lsa.Width != null) stroke = stroke with { Width = lsa.Width };
                            if (lsa.Style != null) stroke = stroke with { Style = lsa.Style.Value };
                            break;
                    }
                });
            }

            for (var i = 0; i < line.Elements.Length - 1; i++)
            {
                var from = line.Elements[i];
                var to = line.Elements[i + 1];

                var fromTarget = FindObject(from.Target);
                var toTarget = FindObject(to.Target);

                if (fromTarget != null && toTarget != null && from.Direction.HasValue)
                {
                    if (from.Direction.Value == ArrowKind.Forward)
                    {
                        lines.Add(new(fromTarget, toTarget, offsetStart, offsetEnd, stroke));
                    }
                    else
                    {
                        lines.Add(new(toTarget, fromTarget, offsetStart, offsetEnd, stroke));
                    }
                }
            }
        }

        private AST.ObjectAttribute[] FindObjectClass(string name)
        {
            if (!objectClasses.ContainsKey(name))
            {
                if (lineClasses.ContainsKey(name))
                {
                    ws.Add(new($"Class '{name}' can only be used for lines, not objects."));
                }
                else
                {
                    ws.Add(new($"Class '{name}' not defined."));
                }
                
                return Array.Empty<AST.ObjectAttribute>();
            }
            else
            {
                return objectClasses[name];
            }
        }

        private AST.LineAttribute[] FindLineClass(string name)
        {
            if (!lineClasses.ContainsKey(name))
            {
                if (objectClasses.ContainsKey(name))
                {
                    ws.Add(new($"Class '{name}' can only be used for objects, not lines."));
                }
                else
                {
                    ws.Add(new($"Class '{name}' not defined."));
                }

                return Array.Empty<AST.LineAttribute>();
            }
            else
            {
                return lineClasses[name];
            }
        }

        private IR.Object? FindObject(string name)
        {
            if (!objects.ContainsKey(name))
            {
                ws.Add(new($"No object found with name '{name}'."));
                return null;
            }

            return objects[name];
        }
    }
}
