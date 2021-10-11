using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Thousand.Model;

namespace Thousand
{
    public class Evaluator
    {
        public static bool TryEvaluate(IEnumerable<AST.TypedDocument> documents, List<GenerationError> warnings, List<GenerationError> errors, [NotNullWhen(true)] out IR.Root? rules)
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
            catch (Exception ex)
            {
                errors.Add(new(ex));
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
            Config = new IR.Config(Colour.White, LayoutKind.Grid, FlowKind.Row, 0, new(5), new(0), new(new EqualSize()), new(AlignmentKind.Center));
        }

        private void AddDocument(AST.TypedDocument diagram)
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
                }, 
                region => Config = ApplyRegionAttributes(Config, region), 
                text => rootFont = ApplyFontAttributes(rootFont, text));
            }

            foreach (var c in diagram.Declarations.Where(d => d.IsT1).Select(d => d.AsT1))
            {
                if (c is AST.ObjectClass || c is AST.ObjectOrLineClass && c.BaseClasses.All(b => objectClasses.ContainsKey(b.Text)))
                {
                    var localAttrs = c switch
                    {
                        AST.ObjectClass oc => oc.Attributes,
                        AST.ObjectOrLineClass olc => olc.Attributes.Select(ea => ea.IsT0 ? new AST.ObjectAttribute(ea.AsT0) : new AST.ObjectAttribute(ea.AsT1)),
                        _ => Enumerable.Empty<AST.ObjectAttribute>()
                    };

                    var allAttrs = c.BaseClasses
                        .SelectMany(FindObjectClass)
                        .Concat(localAttrs)
                        .ToArray();

                    objectClasses[c.Name.Text] = allAttrs;
                }

                if (c is AST.LineClass || c is AST.ObjectOrLineClass && c.BaseClasses.All(b => lineClasses.ContainsKey(b.Text)))
                {
                    var localAttrs = c switch
                    {
                        AST.LineClass lc => lc.Attributes,
                        AST.ObjectOrLineClass olc => olc.Attributes.Select(ea => ea.IsT0 ? new AST.SegmentAttribute(ea.AsT0) : new AST.SegmentAttribute(ea.AsT1)),
                        _ => Enumerable.Empty<AST.SegmentAttribute>()
                    };

                    var allAttrs = c.BaseClasses
                        .SelectMany(FindLineClass)
                        .Concat(localAttrs)
                        .ToArray();

                    lineClasses[c.Name.Text] = allAttrs;
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
            var regionConfig = new IR.Config(null, LayoutKind.Grid, FlowKind.Row, 0, new(15), new(0), new(new PackedSize()), new(AlignmentKind.Start));
            
            var label = node.Name?.Text; // names are a separate thing, but if a node has one, it is also the default label
            var font = cascadeFont;

            var alignment = new IR.Axes<AlignmentKind?>(null, null);
            var margin = new Border(0);
            var width = new int?();
            var height = new int?();

            var column = new int?();
            var row = new int?();           
            var x = new int?();
            var y = new int?();
            var anchor = default(CompassKind?);
            var offset = default(Point?);

            var shape = new ShapeKind?(ShapeKind.Roundrect);
            var cornerRadius = 15;

            var stroke = new Stroke();                        

            foreach (var attr in node.Classes.SelectMany(FindObjectClass).Concat(node.Attributes).Concat(node.Children.Where(d => d.IsT0).Select(d => d.AsT0)))
            {
                attr.Switch(e =>
                {
                    switch (e)
                    {
                        case AST.PositionAnchorAttribute eaa:
                            anchor = eaa.Placement;
                            break;

                        case AST.PositionOffsetAttribute eoa:
                            offset = eoa.Offset;
                            break;
                    }
                }, n =>
                {
                    switch (n)
                    {
                        case AST.NodeLabelAttribute nla:
                            label = nla.Content;
                            break;

                        case AST.NodeColumnAttribute nca:
                            column = nca.Value;
                            break;

                        case AST.NodeRowAttribute nra:
                            row = nra.Value;
                            break;

                        case AST.NodeXAttribute nxa:
                            x = nxa.Value;
                            break;

                        case AST.NodeYAttribute nya:
                            y = nya.Value;
                            break;

                        case AST.NodeMinWidthAttribute nwa:
                            width = nwa.Value;
                            break;

                        case AST.NodeMinHeightAttribute nha:
                            height = nha.Value;
                            break;

                        case AST.NodeShapeAttribute nsa:
                            shape = nsa.Kind;
                            break;

                        case AST.NodeAlignColumnAttribute naca:
                            alignment = alignment with { Columns = naca.Kind };
                            break;

                        case AST.NodeAlignRowAttribute nara:
                            alignment = alignment with { Rows = nara.Kind };
                            break;

                        case AST.NodeAlignAttribute naa:
                            alignment = new(naa.Columns, naa.Rows);
                            break;
                            
                        case AST.NodeMarginAttribute nma:
                            margin = nma.Value;
                            break;

                        case AST.NodeCornerRadiusAttribute ncra:
                            cornerRadius = ncra.Value;
                            break;
                    }
                }, 
                r => regionConfig = ApplyRegionAttributes(regionConfig, r), 
                t => font = ApplyFontAttributes(font, t), 
                l => stroke = ApplyStrokeAttributes(stroke, l));
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

            var result = new IR.Object(new IR.Region(regionConfig, children), label, font, alignment, margin, width, height, row, column, x, y, anchor, offset, shape, cornerRadius, stroke);

            if (node.Name?.Text is string name && allObjects.ContainsKey(name))
            {
                es.Add(new(node.Name.Location, ErrorKind.Reference, $"object `{name}` has already been defined"));
            }
            else
            {
                allObjects.Add(node.Name?.Text ?? Guid.NewGuid().ToString(), result);
            }

            return result;
        }

        private void AddEdge(AST.TypedLine line)
        {
            var stroke = new Stroke();
            var anchorStart = new NoAnchor() as Anchor;
            var anchorEnd = new NoAnchor() as Anchor;
            var offsetStart = Point.Zero;
            var offsetEnd = Point.Zero;

            foreach (var attr in line.Classes.SelectMany(FindLineClass).Concat(line.Attributes))
            {
                attr.Switch(entity =>
                {
                    switch (entity)
                    {
                        case AST.PositionAnchorAttribute eaa:
                            var anchor = new SpecificAnchor(eaa.Placement);
                            anchorStart = anchor;
                            anchorEnd = anchor;
                            break;

                        case AST.PositionOffsetAttribute eoa:
                            offsetStart = eoa.Offset;
                            offsetEnd = eoa.Offset;
                            break;
                    }
                }, arrow =>
                {
                    switch (arrow)
                    {
                        case AST.ArrowAnchorStartAttribute aasa:
                            anchorStart = aasa.Anchor;
                            break;

                        case AST.ArrowAnchorEndAttribute aaea:
                            anchorEnd = aaea.Anchor;
                            break;

                        case AST.ArrowAnchorAttribute aaa:
                            anchorStart = aaa.Start;
                            anchorEnd = aaa.End;
                            break;

                        case AST.ArrowOffsetStartAttribute aosa:
                            offsetStart = aosa.Offset;
                            break;

                        case AST.ArrowOffsetEndAttribute aoea:
                            offsetEnd = aoea.Offset; ;
                            break;

                        case AST.ArrowOffsetAttribute aoa:
                            offsetStart = aoa.Start;
                            offsetEnd = aoa.End;
                            break;
                    }
                }, line => stroke = ApplyStrokeAttributes(stroke, line));
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
                            rootEdges.Add(new(stroke, to.Target, from.Target, toTarget, fromTarget, null, MarkerKind.Arrowhead, anchorStart, anchorEnd, offsetStart, offsetEnd));
                            break;

                        case ArrowKind.Forward:
                            rootEdges.Add(new(stroke, from.Target, to.Target, fromTarget, toTarget, null, MarkerKind.Arrowhead, anchorStart, anchorEnd, offsetStart, offsetEnd));
                            break;

                        case ArrowKind.Neither:
                            rootEdges.Add(new(stroke, from.Target, to.Target, fromTarget, toTarget, null, null, anchorStart, anchorEnd, offsetStart, offsetEnd));
                            break;

                        case ArrowKind.Both:
                            rootEdges.Add(new(stroke, from.Target, to.Target, fromTarget, toTarget, MarkerKind.Arrowhead, MarkerKind.Arrowhead, anchorStart, anchorEnd, offsetStart, offsetEnd));
                            break;
                            
                        default:
                            es.Add(new(from.Target.Location, ErrorKind.Internal, $"unknown ArrowKind {from.Direction.Value} from object `{from.Target.Text}`"));
                            break;
                    }
                }
            }
        }

        private IR.Config ApplyRegionAttributes(IR.Config config, AST.RegionAttribute attribute)
        {
            return attribute switch
            {
                AST.RegionFillAttribute rfa => config with { Fill = rfa.Colour },
                AST.RegionPaddingAttribute rpa => config with { Padding = rpa.Value },
                AST.RegionLayoutAttribute rla => config with { Layout = rla.Kind },
                AST.RegionGridFlowAttribute rgfa => config with { GridFlow = rgfa.Kind },
                AST.RegionGridMaxAttribute rgma => config with { GridMax = rgma.Value },
                AST.RegionGridAttribute rga => config with { GridFlow = rga.Flow ?? config.GridFlow, GridMax = rga.Max ?? config.GridMax },
                AST.RegionSpaceColumnsAttribute rsca => config with { Gutter = new(rsca.Value, config.Gutter.Rows) },
                AST.RegionSpaceRowsAttribute rsra => config with { Gutter = new(config.Gutter.Columns, rsra.Value) },
                AST.RegionSpaceAttribute rsa => config with { Gutter = new(rsa.Columns, rsa.Rows) },
                AST.RegionPackColumnsAttribute rpca => config with { Size = new(rpca.Size, config.Size.Rows) },
                AST.RegionPackRowsAttribute rpra => config with { Size = new(config.Size.Columns, rpra.Size) },
                AST.RegionPackAttribute rpa => config with { Size = new(rpa.Columns, rpa.Rows) },
                AST.RegionJustifyColumnsAttribute rjca => config with { Alignment = new(rjca.Kind, config.Alignment.Rows) },
                AST.RegionJustifyRowsAttribute rjra => config with { Alignment = new(config.Alignment.Columns, rjra.Kind) },
                AST.RegionJustifyAttribute rja => config with { Alignment = new(rja.Columns, rja.Rows) },
                _ => config
            };
        }

        private Font ApplyFontAttributes(Font font, AST.TextAttribute attribute)
        {
            return attribute switch
            {
                AST.TextFontFamilyAttribute tffa => font with { Family = tffa.Name },
                AST.TextFontSizeAttribute tfsa => font with { Size = tfsa.Value },
                AST.TextFontColourAttribute tfca => font with { Colour = tfca.Colour },
                AST.TextFontAttribute tfa => new Font
                {
                    Family = tfa.Family ?? font.Family,
                    Size = tfa.Size ?? font.Size,
                    Colour = tfa.Colour ?? font.Colour
                },
                _ => font
            };
        }

        private Stroke ApplyStrokeAttributes(Stroke stroke, AST.LineAttribute attribute)
        {
            return attribute switch
            {
                AST.LineStrokeColourAttribute lsca => stroke with { Colour = lsca.Colour },
                AST.LineStrokeWidthAttribute lswa => stroke with { Width = lswa.Value },
                AST.LineStrokeStyleAttribute lssa => stroke with { Style = lssa.Kind },
                AST.LineStrokeAttribute lsa => new Stroke
                {
                    Colour = lsa.Colour ?? stroke.Colour,
                    Width = lsa.Width ?? stroke.Width,
                    Style = lsa.Style ?? stroke.Style
                },
                _ => stroke
            };
        }

        private AST.ObjectAttribute[] FindObjectClass(Parse.Identifier name)
        {
            if (!objectClasses.ContainsKey(name.Text))
            {
                if (lineClasses.ContainsKey(name.Text))
                {
                    ws.Add(new(name.Location, ErrorKind.Type, $"class `{name.Text}` can only be used for lines, not objects"));
                }
                else
                {
                    ws.Add(new(name.Location, ErrorKind.Type, $"class `{name.Text}` is not defined"));
                }
                
                return Array.Empty<AST.ObjectAttribute>();
            }
            else
            {
                return objectClasses[name.Text];
            }
        }

        private AST.SegmentAttribute[] FindLineClass(Parse.Identifier name)
        {
            if (!lineClasses.ContainsKey(name.Text))
            {
                if (objectClasses.ContainsKey(name.Text))
                {
                    ws.Add(new(name.Location, ErrorKind.Type, $"class `{name.Text}` can only be used for objects, not lines"));
                }
                else
                {
                    ws.Add(new(name.Location, ErrorKind.Type, $"class `{name.Text}` is not defined"));
                }

                return Array.Empty<AST.SegmentAttribute>();
            }
            else
            {
                return lineClasses[name.Text];
            }
        }

        private IR.Object? FindObject(Parse.Identifier name)
        {
            if (!allObjects.ContainsKey(name.Text))
            {
                ws.Add(new(name.Location, ErrorKind.Reference, $"object `{name.Text}` is not defined"));
                return null;
            }

            return allObjects[name.Text];
        }
    }
}
