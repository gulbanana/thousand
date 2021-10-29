using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Thousand.Model;

namespace Thousand.Evaluate
{
    public class Evaluator
    {
        public static bool TryEvaluate(IEnumerable<AST.TypedDocument> documents, GenerationState state, [NotNullWhen(true)] out IR.Region? root)
        {
            try
            {
                var errors = state.ErrorCount();

                var evaluation = new Evaluator(state);
                foreach (var doc in documents)
                {
                    evaluation.AddDocument(doc);
                }
                root = new IR.Region(evaluation.Config, evaluation.Entities);

                return state.ErrorCount() == errors;
            }
            catch (Exception ex)
            {
                state.AddError(ex);
                root = null;
                return false;
            }
        }

        private readonly GenerationState state;
        private readonly Scope rootScope;
        private readonly List<IR.Entity> rootEntities;
        private Font rootFont;

        public IR.Config Config { get; private set; }
        public IReadOnlyList<IR.Entity> Entities => rootEntities;

        private Evaluator(GenerationState state)
        {
            this.state = state;
            rootScope = new("root", state);
            rootEntities = new();

            rootFont = new Font();
            Config = new IR.Config();
        }

        private void AddDocument(AST.TypedDocument diagram)
        {
            foreach (var attr in diagram.Declarations.Where(d => d.IsT0).Select(d => d.AsT0))
            {
                attr.Switch(region => Config = ApplyRegionAttributes(Config, region),
                text => rootFont = ApplyFontAttributes(rootFont, text));
            }
            
            foreach (var declaration in diagram.Declarations)
            {
                if (declaration.IsT1)
                {
                    AddClass(declaration.AsT1, rootScope);
                }
                else if (declaration.IsT2)
                {
                    rootEntities.Add(AddObject(declaration.AsT2, rootFont, rootScope));
                }
                else if (declaration.IsT3)
                {
                    var (objects, edges) = AddLine(declaration.AsT3, rootFont, rootScope);
                    rootEntities.AddRange(objects);
                    rootEntities.AddRange(edges);
                }
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
                    AST.ObjectOrLineClass olc => olc.Attributes.Select(ea => (AST.ObjectAttribute)ea),
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
                    AST.ObjectOrLineClass olc => olc.Attributes.Select(ea => (AST.LineAttribute)ea),
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

            var regionConfig = new IR.Config();

            // names are a separate thing, but if a node has one, it is also the default label
            var shared = new SharedStyles(node.Name?.Text, AlignmentKind.Center, new Stroke());
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

            foreach (var attr in node.Classes.SelectMany(c => scope.FindObjectClass(c, true).Attributes)
                .Concat(node.Classes.SelectMany(c => scope.FindObjectClass(c, false).Children.Where(d => d.IsT0).Select(d => d.AsT0)))
                .Concat(node.Attributes)
                .Concat(node.Declarations.Where(d => d.IsT0).Select(d => d.AsT0)))
            {
                attr.Switch(e =>
                {
                    switch (e)
                    {
                        case AST.EntityAnchorAttribute eaa:
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

                        case AST.EntityOffsetAttribute eoa:
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

                    shared = ApplySharedAttributes(shared, e);
                }, n =>
                {
                    switch (n)
                    {
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
                t => font = ApplyFontAttributes(font, t));
            }

            var childEntities = new List<IR.Entity>();
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
                        childEntities.Add(AddObject(declaration.AsT2, font, objectScope));
                    }
                    else if (declaration.IsT3)
                    {
                        var (objects, edges) = AddLine(declaration.AsT3, font, objectScope);
                        childEntities.AddRange(objects);
                        childEntities.AddRange(edges);
                    }
                }
            }

            var result = new IR.Object(
                name,
                new IR.Region(regionConfig, childEntities), 
                shared.Label == null ? null : new IR.StyledText(font, shared.Label, shared.JustifyLabel), 
                alignment, margin, width, height, 
                row, column, position, anchor, offset, 
                shape, cornerRadius, 
                shared.Stroke
            );

            scope.AddObject(node.Name, result);

            return result;
        }

        private (IEnumerable<IR.Object>, IEnumerable<IR.Edge>) AddLine(AST.TypedLine line, Font cascadeFont, Scope scope)
        {
            var objects = new List<IR.Object>();
            var edges = new List<IR.Edge>();

            var shared = new SharedStyles(null, AlignmentKind.Center, new Stroke());
            var font = cascadeFont;
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
                        case AST.EntityAnchorAttribute eaa:
                            anchorStart = eaa.Start;
                            anchorEnd = eaa.End;
                            break;

                        case AST.EntityOffsetAttribute eoa:
                            offsetStart = eoa.Start;
                            offsetEnd = eoa.End;
                            break;
                    }

                    shared = ApplySharedAttributes(shared, entity);
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
                }, text => font = ApplyFontAttributes(font, text));
            }

            var nodes = new List<(ArrowKind? direction, IR.Object? target)>();
            foreach (var seg in line.Segments)
            {
                var target = seg.Target.IsT0 ? scope.FindObject(seg.Target.AsT0) : AddObject(seg.Target.AsT1, cascadeFont, scope);
                if (seg.Target.IsT1)
                {
                    objects.Add(target!);
                }
                nodes.Add((seg.Direction, target));
            }

            var label = shared.Label == null ? null : new IR.StyledText(font, shared.Label, shared.JustifyLabel);
            for (var i = 0; i < nodes.Count - 1; i++)
            {
                var from = nodes[i];
                var to = nodes[i + 1];
                
                if (from.target != null && to.target != null && from.direction.HasValue)
                {
                    var fromName = from.target.Name;
                    var toName = from.target.Name;

                    switch (from.direction.Value)
                    {
                        case ArrowKind.Backward:
                            edges.Add(new(
                                new IR.Endpoint(toName, to.target, null, anchorStart, offsetStart), 
                                new IR.Endpoint(fromName, from.target, MarkerKind.Arrowhead, anchorEnd, offsetEnd), 
                                shared.Stroke, 
                                label
                            ));
                            break;

                        case ArrowKind.Forward:
                            edges.Add(new(
                                new IR.Endpoint(fromName, from.target, null, anchorStart, offsetStart),
                                new IR.Endpoint(toName, to.target, MarkerKind.Arrowhead, anchorEnd, offsetEnd),
                                shared.Stroke,
                                label
                            ));
                            break;

                        case ArrowKind.Neither:
                            edges.Add(new(
                                new IR.Endpoint(fromName, from.target, null, anchorStart, offsetStart),
                                new IR.Endpoint(toName, to.target, null, anchorEnd, offsetEnd),
                                shared.Stroke,
                                label
                            ));
                            break;

                        case ArrowKind.Both:
                            edges.Add(new(
                                new IR.Endpoint(fromName, from.target, MarkerKind.Arrowhead, anchorStart, offsetStart),
                                new IR.Endpoint(toName, to.target, MarkerKind.Arrowhead, anchorEnd, offsetEnd),
                                shared.Stroke,
                                label
                            ));
                            break;
                            
                        default:
                            state.AddError(fromName, ErrorKind.Internal, $"unknown ArrowKind {from.direction.Value} from object {0}", fromName);
                            break;
                    }
                }
            }

            return (objects, edges);
        }

        private IR.Config ApplyRegionAttributes(IR.Config config, AST.RegionAttribute attribute)
        {
            return attribute switch
            {
                AST.RegionScaleAttribute rsa => config with { Scale = rsa.Value },
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

        private SharedStyles ApplySharedAttributes(SharedStyles shared, AST.EntityAttribute attribute)
        {
            return attribute switch
            {
                AST.EntityStrokeColourAttribute ssca => shared with { Stroke = shared.Stroke with { Colour = ssca.Colour } },
                AST.EntityStrokeWidthAttribute sswa => shared with { Stroke = shared.Stroke with { Width = sswa.Value } },
                AST.EntityStrokeStyleAttribute sssa => shared with { Stroke = shared.Stroke with { Style = sssa.Kind } },
                AST.EntityStrokeAttribute ssa => shared with
                {
                    Stroke = new Stroke
                    {
                        Colour = ssa.Colour ?? shared.Stroke.Colour,
                        Width = ssa.Width ?? shared.Stroke.Width,
                        Style = ssa.Style ?? shared.Stroke.Style
                    }
                },

                AST.EntityLabelContentAttribute slca => shared with { Label = slca.Content },
                AST.EntityLabelJustifyAttribute slja => shared with { JustifyLabel = slja.Kind },
                AST.EntityLabelAttribute sla => shared with
                {
                    Label = sla.Content.HasValue ? sla.Content.Value.Value : shared.Label,
                    JustifyLabel = sla.Justify ?? shared.JustifyLabel
                },

                _ => shared
            };
        }
    }
}
