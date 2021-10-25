using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Thousand.Model;

namespace Thousand.Evaluate
{
    public class Evaluator
    {
        public static bool TryEvaluate(IEnumerable<AST.TypedDocument> documents, GenerationState state, [NotNullWhen(true)] out IR.Root? rules)
        {
            try
            {
                var errors = state.ErrorCount();

                var evaluation = new Evaluator(state);
                foreach (var doc in documents)
                {
                    evaluation.AddDocument(doc);
                }
                rules = new IR.Root(evaluation.Scale, new IR.Region(evaluation.Config, evaluation.Objects.ToList()), evaluation.Edges);

                return state.ErrorCount() == errors;
            }
            catch (Exception ex)
            {
                state.AddError(ex);
                rules = null;
                return false;
            }
        }

        private readonly GenerationState state;
        private readonly Scope rootScope;
        private readonly List<IR.Edge> rootEdges;

        private Font rootFont;
        public decimal Scale { get; private set; }
        public IR.Config Config { get; private set; }

        public IEnumerable<IR.Object> Objects => rootScope.GetObjects();
        public IReadOnlyList<IR.Edge> Edges => rootEdges;

        private Evaluator(GenerationState state)
        {
            this.state = state;
            rootScope = new("diagram", state);
            rootEdges = new();

            rootFont = new Font();
            Scale = 1m;
            Config = new IR.Config(Colour.White, FlowKind.Auto, 0, new(5), new(0), new(new EqualContentSize()), new(AlignmentKind.Center));
        }

        private void AddDocument(AST.TypedDocument diagram)
        {
            foreach (var attr in diagram.Declarations.Where(d => d.IsT0).Select(d => d.AsT0))
            {
                attr.Switch(doc =>
                {
                    switch (doc)
                    {
                        case AST.DiagramScaleAttribute dsa:
                            Scale = dsa.Value;
                            break;
                    }
                }, 
                region => Config = ApplyRegionAttributes(Config, region), 
                text => rootFont = ApplyFontAttributes(rootFont, text));
            }

            foreach (var c in diagram.Declarations.Where(d => d.IsT1).Select(d => d.AsT1))
            {
                AddClass(c, rootScope);
            }

            foreach (var node in diagram.Declarations.Where(d => d.IsT2).Select(d => d.AsT2))
            {
                AddObject(node, rootFont, rootScope);
            }

            foreach (var chain in diagram.Declarations.Where(d => d.IsT3).Select(d => d.AsT3))
            {
                AddEdges(chain, rootFont, rootScope).ToList();
            }
        }

        private void AddClass(AST.TypedClass c, Scope scope)
        {
            foreach (var b in c.BaseClasses)
            {
                if (!scope.HasRequiredClass(b))
                {
                    return;
                }
            }

            if (c is AST.ObjectClass || c is AST.ObjectOrLineClass && c.BaseClasses.All(b => scope.FindObjectClass(b, false).Found))
            {
                var localAttrs = c switch
                {
                    AST.ObjectClass oc => oc.Attributes,
                    AST.ObjectOrLineClass olc => olc.Attributes.Select(ea => ea.IsT0 ? new AST.ObjectAttribute(ea.AsT0) : new AST.ObjectAttribute(ea.AsT1)),
                    _ => Enumerable.Empty<AST.ObjectAttribute>()
                };

                var localChildren = c switch
                {
                    AST.ObjectClass oc => oc.Declarations,
                    _ => Enumerable.Empty<AST.TypedObjectContent>()
                };

                var allAttrs = c.BaseClasses
                    .SelectMany(b => scope.FindObjectClass(b, true).Attributes)
                    .Concat(localAttrs)
                    .ToArray();

                var allChildren = c.BaseClasses
                    .SelectMany(b => scope.FindObjectClass(b, false).Children)
                    .Concat(localChildren)
                    .ToArray();

                scope.AddObjectClass(c.Name.Text, new(true, allAttrs, allChildren));
            }

            if (c is AST.LineClass || c is AST.ObjectOrLineClass && c.BaseClasses.All(b => scope.FindLineClass(b, false).Found))
            {
                var localAttrs = c switch
                {
                    AST.LineClass lc => lc.Attributes,
                    AST.ObjectOrLineClass olc => olc.Attributes.Select(ea => ea.IsT0 ? new AST.LineAttribute(ea.AsT0) : new AST.LineAttribute(ea.AsT1)),
                    _ => Enumerable.Empty<AST.LineAttribute>()
                };

                var allAttrs = c.BaseClasses
                    .SelectMany(b => scope.FindLineClass(b, true).Attributes)
                    .Concat(localAttrs)
                    .ToArray();

                scope.AddLineClass(c.Name.Text, new(true, allAttrs));
            }
        }

        private IR.Object AddObject(AST.TypedObject node, Font cascadeFont, Scope scope)
        {
            var name = node.Name ?? new Parse.Identifier(node.TypeSpan.ToStringValue()) { Span = node.TypeSpan };
            
            var regionConfig = new IR.Config(null, FlowKind.Auto, 0, new(15), new(0), new(new PackedSize()), new(AlignmentKind.Start));
            
            var label = new Text(node.Name?.Text); // names are a separate thing, but if a node has one, it is also the default label
            var justifyText = AlignmentKind.Center;
            var font = cascadeFont;

            var alignment = new IR.Axes<AlignmentKind?>(null, null);
            var margin = new Border(0);
            var width = new decimal?();
            var height = new decimal?();

            var column = new int?();
            var row = new int?();
            var position = default(Point?);
            var anchor = default(CompassKind?);
            var offset = default(Point?);

            var shape = new ShapeKind?(ShapeKind.Roundrect);
            var cornerRadius = 15;

            var stroke = new Stroke();                        

            foreach (var attr in node.Classes.SelectMany(c => scope.FindObjectClass(c, true).Attributes)
                .Concat(node.Classes.SelectMany(c => scope.FindObjectClass(c, false).Children.Where(d => d.IsT0).Select(d => d.AsT0)))
                .Concat(node.Attributes)
                .Concat(node.Declarations.Where(d => d.IsT0).Select(d => d.AsT0)))
            {
                attr.Switch(e =>
                {
                    switch (e)
                    {
                        case AST.PositionAnchorAttribute eaa:
                            if (eaa.Start is SpecificAnchor { Kind: var placement } && eaa.End.Equals(eaa.Start))
                            {
                                anchor = placement;
                            }
                            else
                            {
                                // XXX save attr identifiers in the final AST
                                state.AddError(name, ErrorKind.Type, "object {0} has too many anchors (expected compass direction)", name);
                            }
                            break;

                        case AST.PositionOffsetAttribute eoa:
                            if (eoa.End.Equals(eoa.Start))
                            {
                                offset = eoa.Start;
                            }
                            else 
                            {
                                // XXX save attr identifiers in the final AST
                                state.AddError(name, ErrorKind.Type, "object {0} has too many offsets (expected point)", name);
                            }
                            break;
                    }
                }, n =>
                {
                    switch (n)
                    {
                        case AST.NodeLabelContentAttribute nlca:
                            label = nlca.Text;
                            break;

                        case AST.NodeLabelJustifyAttribute nlja:
                            justifyText = nlja.Kind;
                            break;

                        case AST.NodeLabelAttribute nla:
                            label = nla.Content ?? label;
                            justifyText = nla.Justify ?? justifyText;
                            break;

                        case AST.NodeColumnAttribute nca:
                            column = nca.Value;
                            break;

                        case AST.NodeRowAttribute nra:
                            row = nra.Value;
                            break;

                        case AST.NodePositionAttribute nxa:
                            position = nxa.Origin;
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

            var childObjects = new List<IR.Object>();
            var childContent = node.Classes.SelectMany(c => scope.FindObjectClass(c, true).Children).Concat(node.Declarations).ToList();            
            if (childContent.Any())
            {
                var objectScope = scope.Chain(node.Name?.Text ?? node.Classes.First().Text);

                foreach (var declaration in childContent)
                {
                    if (declaration.IsT1)
                    {
                        AddClass(declaration.AsT1, objectScope);
                    }
                    else if (declaration.IsT2)
                    {
                        childObjects.Add(AddObject(declaration.AsT2, font, objectScope));
                    }
                    else if (declaration.IsT3)
                    {
                        childObjects.AddRange(AddEdges(declaration.AsT3, font, objectScope));
                    }
                }
            }

            var result = new IR.Object(
                name,
                new IR.Region(regionConfig, childObjects), 
                label.Value == null ? null : new IR.StyledText(font, label.Value, justifyText), 
                alignment, margin, width, height, 
                row, column, position, anchor, offset, 
                shape, cornerRadius, 
                stroke
            );

            scope.AddObject(node.Name, result);

            return result;
        }

        private IEnumerable<IR.Object> AddEdges(AST.TypedLine line, Font cascadeFont, Scope scope)
        {
            var stroke = new Stroke();
            var anchorStart = new NoAnchor() as Anchor;
            var anchorEnd = new NoAnchor() as Anchor;
            var offsetStart = Point.Zero;
            var offsetEnd = Point.Zero;

            foreach (var attr in line.Classes.SelectMany(c => scope.FindLineClass(c, true).Attributes).Concat(line.Attributes))
            {
                attr.Switch(entity =>
                {
                    switch (entity)
                    {
                        case AST.PositionAnchorAttribute eaa:
                            anchorStart = eaa.Start;
                            anchorEnd = eaa.End;
                            break;

                        case AST.PositionOffsetAttribute eoa:
                            offsetStart = eoa.Start;
                            offsetEnd = eoa.End;
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

                        case AST.ArrowOffsetStartAttribute aosa:
                            offsetStart = aosa.Offset;
                            break;

                        case AST.ArrowOffsetEndAttribute aoea:
                            offsetEnd = aoea.Offset; ;
                            break;
                    }
                }, line => stroke = ApplyStrokeAttributes(stroke, line));
            }

            var resolvedSegments = new List<(ArrowKind? direction, IR.Object? target)>();
            foreach (var seg in line.Segments)
            {
                var target = seg.Target.IsT0 ? scope.FindObject(seg.Target.AsT0) : AddObject(seg.Target.AsT1, cascadeFont, scope);
                if (seg.Target.IsT1)
                {
                    yield return target!;
                }
                resolvedSegments.Add((seg.Direction, target));
            }

            for (var i = 0; i < resolvedSegments.Count - 1; i++)
            {
                var from = resolvedSegments[i];
                var to = resolvedSegments[i + 1];
                
                if (from.target != null && to.target != null && from.direction.HasValue)
                {
                    var fromName = from.target.Name;
                    var toName = from.target.Name;

                    switch (from.direction.Value)
                    {
                        case ArrowKind.Backward:
                            rootEdges.Add(new(stroke, toName, fromName, to.target, from.target, null, MarkerKind.Arrowhead, anchorStart, anchorEnd, offsetStart, offsetEnd));
                            break;

                        case ArrowKind.Forward:
                            rootEdges.Add(new(stroke, fromName, toName, from.target, to.target, null, MarkerKind.Arrowhead, anchorStart, anchorEnd, offsetStart, offsetEnd));
                            break;

                        case ArrowKind.Neither:
                            rootEdges.Add(new(stroke, fromName, toName, from.target, to.target, null, null, anchorStart, anchorEnd, offsetStart, offsetEnd));
                            break;

                        case ArrowKind.Both:
                            rootEdges.Add(new(stroke, fromName, toName, from.target, to.target, MarkerKind.Arrowhead, MarkerKind.Arrowhead, anchorStart, anchorEnd, offsetStart, offsetEnd));
                            break;
                            
                        default:
                            state.AddError(fromName, ErrorKind.Internal, $"unknown ArrowKind {from.direction.Value} from object {0}", fromName);
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
                AST.RegionGridFlowAttribute rgfa => config with { GridFlow = rgfa.Kind },
                AST.RegionGridMaxAttribute rgma => config with { GridMax = rgma.Value },
                AST.RegionGridAttribute rga => config with { GridFlow = rga.Flow ?? config.GridFlow, GridMax = rga.Max ?? config.GridMax },
                AST.RegionSpaceColumnsAttribute rsca => config with { Gutter = new(rsca.Value, config.Gutter.Rows) },
                AST.RegionSpaceRowsAttribute rsra => config with { Gutter = new(config.Gutter.Columns, rsra.Value) },
                AST.RegionSpaceAttribute rsa => config with { Gutter = new(rsa.Columns, rsa.Rows) },
                AST.RegionLayoutColumnsAttribute rpca => config with { Layout = new(rpca.Size, config.Layout.Rows) },
                AST.RegionLayoutRowsAttribute rpra => config with { Layout = new(config.Layout.Columns, rpra.Size) },
                AST.RegionLayoutAttribute rpa => config with { Layout = new(rpa.Columns, rpa.Rows) },
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

        private Stroke ApplyStrokeAttributes(Stroke stroke, AST.StrokeAttribute attribute)
        {
            return attribute switch
            {
                AST.StrokeColourAttribute lsca => stroke with { Colour = lsca.Colour },
                AST.StrokeWidthAttribute lswa => stroke with { Width = lswa.Value },
                AST.StrokeStyleAttribute lssa => stroke with { Style = lssa.Kind },
                AST.StrokeShorthandAttribute lsa => new Stroke
                {
                    Colour = lsa.Colour ?? stroke.Colour,
                    Width = lsa.Width ?? stroke.Width,
                    Style = lsa.Style ?? stroke.Style
                },
                _ => stroke
            };
        }
    }
}
