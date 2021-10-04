using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Thousand.Model;

namespace Thousand
{
    public class Evaluator
    {
        public static bool TryEvaluate(IEnumerable<AST.Document> documents, List<GenerationError> warnings, List<GenerationError> errors, [NotNullWhen(true)] out IR.Root? rules)
        {
            try
            {
                var evaluation = new Evaluator(warnings, errors);
                foreach (var doc in documents)
                {
                    evaluation.AddDocument(doc);
                }
                rules = new IR.Root(evaluation.Scale, new IR.Region(evaluation.Config, evaluation.Objects), evaluation.Edges);

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
        private readonly Dictionary<string, AST.SegmentAttribute[]> lineClasses;
        private readonly Dictionary<string, IR.Object> allObjects;
        private readonly List<IR.Object> rootObjects;
        private readonly List<IR.Edge> rootEdges;

        private Font rootFont;
        public decimal Scale { get; private set; }
        public IR.Config Config { get; private set; }
        public IReadOnlyList<IR.Object> Objects => rootObjects;
        public IReadOnlyList<IR.Edge> Edges => rootEdges;

        private Evaluator(List<GenerationError> warnings, List<GenerationError> errors)
        {
            ws = warnings;
            es = errors;

            objectClasses = new(StringComparer.OrdinalIgnoreCase);
            lineClasses = new(StringComparer.OrdinalIgnoreCase);
            allObjects = new(StringComparer.OrdinalIgnoreCase);
            rootObjects = new();
            rootEdges = new();

            rootFont = new Font();
            Scale = 1m;
            Config = new IR.Config(LayoutKind.Grid, 5, 0, Colour.White);
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
                            Scale = dsa.Value;
                            break;
                    }
                }, region =>
                {
                    switch (region)
                    {
                        case AST.RegionFillAttribute rfa:
                            Config = Config with { Fill = rfa.Colour };
                            break;

                        case AST.RegionLayoutAttribute rla:
                            Config = Config with  { Layout = rla.Kind };
                            break;

                        case AST.RegionPaddingAttribute rpa:
                            Config = Config with { Padding = rpa.Value };
                            break;

                        case AST.RegionGutterAttribute rga:
                            Config = Config with { Gutter = rga.Value };
                            break;
                    }
                }, text =>
                {
                    switch (text)
                    {
                        case AST.TextFontFamilyAttribute tffa:
                            rootFont = rootFont with { Family = tffa.Name };
                            break;

                        case AST.TextFontSizeAttribute tfsa:
                            rootFont = rootFont with { Size = tfsa.Value };
                            break;

                        case AST.TextFontColourAttribute tfca:
                            rootFont = rootFont with { Colour = tfca.Colour };
                            break;

                        case AST.TextFontAttribute tfa:
                            rootFont = new Font
                            {
                                Family = tfa.Family ?? rootFont.Family,
                                Size = tfa.Size ?? rootFont.Size,
                                Colour = tfa.Colour ?? rootFont.Colour
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
                        AST.ObjectOrLineClass olc => olc.Attributes.Select(ec => new AST.SegmentAttribute(ec)),
                        _ => Enumerable.Empty<AST.SegmentAttribute>()
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
                rootObjects.Add(AddObject(node, rootFont));
            }

            foreach (var chain in diagram.Declarations.Where(d => d.IsT3).Select(d => d.AsT3))
            {
                AddEdge(chain);
            }
        }

        private IR.Object AddObject(AST.TypedObject node, Font cascadeFont)
        {
            var regionConfig = new IR.Config(LayoutKind.Grid, 15, 0, null);
            
            var label = node.Name; // names are a separate thing, but if a node has one, it is also the default label
            var font = cascadeFont;

            var margin = 0;
            var row = new int?();
            var column = new int?();
            var width = new int?();
            var height = new int?();

            var shape = new ShapeKind?(ShapeKind.Roundrect);
            var cornerRadius = 5;

            var stroke = new Stroke();                        

            foreach (var attr in node.Classes.SelectMany(FindObjectClass).Concat(node.Attributes).Concat(node.Children.Where(d => d.IsT0).Select(d => d.AsT0)))
            {
                attr.Switch(n =>
                {
                    switch (n)
                    {
                        case AST.NodeLabelAttribute nla:
                            label = nla.Content;
                            break;

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

                        case AST.NodeMarginAttribute nma:
                            margin = nma.Value;
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
                            regionConfig = regionConfig with { Fill = rfa.Colour };
                            break;

                        case AST.RegionLayoutAttribute rla:
                            regionConfig = regionConfig with { Layout = rla.Kind };
                            break;

                        case AST.RegionPaddingAttribute rpa:
                            regionConfig = regionConfig with { Padding = rpa.Value };
                            break;

                        case AST.RegionGutterAttribute rga:
                            regionConfig = regionConfig with { Gutter = rga.Value };
                            break;
                    }
                }, t =>
                {
                    switch (t)
                    {
                        case AST.TextFontFamilyAttribute tffa:
                            font = font with { Family = tffa.Name };
                            break;

                        case AST.TextFontSizeAttribute tfsa:
                            font = font with { Size = tfsa.Value };
                            break;

                        case AST.TextFontColourAttribute tfca:
                            font = font with { Colour = tfca.Colour };
                            break;

                        case AST.TextFontAttribute tfa:
                            font = new Font 
                            { 
                                Family = tfa.Family ?? font.Family,
                                Size = tfa.Size ?? font.Size,
                                Colour = tfa.Colour ?? font.Colour
                            };
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
                });
            }

            var children = new List<IR.Object>();
            if (node.Children.Any())
            {
                foreach (var child in node.Children.Where(d => d.IsT1).Select(d => d.AsT1))
                {
                    children.Add(AddObject(child, font));
                }

                foreach (var chain in node.Children.Where(d => d.IsT2).Select(d => d.AsT2))
                {
                    AddEdge(chain);
                }
            }

            var result = new IR.Object(new IR.Region(regionConfig, children), label, font, margin, row, column, width, height, shape, cornerRadius, stroke);

            var name = node.Name ?? Guid.NewGuid().ToString();
            if (allObjects.ContainsKey(name))
            {
                es.Add(new GenerationError($"Duplicate object name '{name}'."));
            }
            else
            {
                allObjects.Add(name, result);
            }

            return result;
        }

        private void AddEdge(AST.TypedLine line)
        {
            var stroke = new Stroke();
            var anchorStart = new AnchorKind?();
            var anchorEnd = new AnchorKind?();
            var offsetStart = Point.Zero;
            var offsetEnd = Point.Zero;

            foreach (var attr in line.Classes.SelectMany(FindLineClass).Concat(line.Attributes))
            {
                attr.Switch(arrow =>
                {
                    switch (arrow)
                    {
                        case AST.ArrowAnchorStartAttribute aasa:
                            anchorStart = aasa.Kind;
                            break;

                        case AST.ArrowAnchorEndAttribute aaea:
                            anchorEnd = aaea.Kind;
                            break;

                        case AST.ArrowAnchorAttribute aaa:
                            anchorStart = aaa.Start;
                            anchorEnd = aaa.End;
                            break;

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
                            offsetStart = offsetStart with { X = aoa.Start };
                            offsetEnd = offsetEnd with { X = aoa.End };
                            break;

                        case AST.ArrowOffsetYAttribute aoa:
                            offsetStart = offsetStart with { Y = aoa.Start };
                            offsetEnd = offsetEnd with { Y = aoa.End };
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

            for (var i = 0; i < line.Segments.Length - 1; i++)
            {
                var from = line.Segments[i];
                var to = line.Segments[i + 1];

                var fromTarget = FindObject(from.Target);
                var toTarget = FindObject(to.Target);

                if (fromTarget != null && toTarget != null && from.Direction.HasValue)
                {
                    switch (from.Direction.Value)
                    {
                        case ArrowKind.Backward:
                            rootEdges.Add(new(stroke, toTarget, fromTarget, null, MarkerKind.Arrowhead, anchorStart, anchorEnd, offsetStart, offsetEnd));
                            break;

                        case ArrowKind.Forward:
                            rootEdges.Add(new(stroke, fromTarget, toTarget, null, MarkerKind.Arrowhead, anchorStart, anchorEnd, offsetStart, offsetEnd));
                            break;

                        case ArrowKind.Neither:
                            rootEdges.Add(new(stroke, fromTarget, toTarget, null, null, anchorStart, anchorEnd, offsetStart, offsetEnd));
                            break;

                        case ArrowKind.Both:
                            rootEdges.Add(new(stroke, fromTarget, toTarget, MarkerKind.Arrowhead, MarkerKind.Arrowhead, anchorStart, anchorEnd, offsetStart, offsetEnd));
                            break;

                        default:
                            es.Add(new GenerationError($"Unknown ArrowKind {from.Direction.Value}"));
                            break;
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

        private AST.SegmentAttribute[] FindLineClass(string name)
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

                return Array.Empty<AST.SegmentAttribute>();
            }
            else
            {
                return lineClasses[name];
            }
        }

        private IR.Object? FindObject(string name)
        {
            if (!allObjects.ContainsKey(name))
            {
                ws.Add(new($"No object found with name '{name}'."));
                return null;
            }

            return allObjects[name];
        }
    }
}
