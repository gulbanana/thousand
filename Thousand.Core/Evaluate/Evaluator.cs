﻿using Superpower.Model;
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
        private readonly TypedScope rootScope;
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
            foreach (var declaration in diagram.Declarations)
            {
                switch (declaration)
                {
                    case AST.TypedClass c:
                        AddClass(c, rootScope);
                        break;

                    case AST.TypedObject o:
                        rootEntities.Add(AddObject(o, rootFont, rootScope, true));
                        break;

                    case AST.TypedLine l:
                        var (objects, edges) = AddLine(l, rootFont, rootScope, true);
                        rootEntities.AddRange(objects);
                        rootEntities.AddRange(edges);
                        break;
                }
            }
        }

        private void AddClass(AST.TypedClass c, TypedScope scope)
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
                    _ => Enumerable.Empty<AST.TypedDeclaration>()
                };

                var allAttrs = c.BaseClasses
                    .SelectMany(b => scope.FindObjectClass(b, true).Attributes)
                    .Concat(localAttrs)
                    .ToArray();

                var allChildren = c.BaseClasses
                    .SelectMany(b => scope.FindObjectClass(b, false).Children)
                    .Concat(localChildren)
                    .ToArray();

                scope.AddObjectClass(c.Name.AsKey, new(true, allAttrs, allChildren));
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

                scope.AddLineClass(c.Name.AsKey, new(true, allAttrs));
            }
        }

        private IR.Node AddObject(AST.TypedObject node, Font cascadeFont, TypedScope scope, bool local)
        {
            var className = state.UnmapSpan(node.Classes[0].AsLoc);
            if (node.Classes.Length > 1 && className.Source != null && className.Source.Length > className.Length)
            {
                var lastClass = state.UnmapSpan(node.Classes.Last().AsLoc);
                var totalLength = lastClass.Position.Absolute - className.Position.Absolute + lastClass.Length;
                className = new TextSpan(className.Source!, className.Position, totalLength);
            }
            
            var displayName = node.Name ?? new Name(className.ToStringValue(), className);

            var regionConfig = new IR.Config();

            // names are a separate thing, but if a node has one, it is also the default label
            var shared = new SharedStyles(node.Name?.AsKey, AlignmentKind.Center, new Stroke());
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
            var cornerRadius = 15m;

            foreach (var attr in node.Classes.SelectMany(c => scope.FindObjectClass(c, true).Attributes).Concat(node.Attributes))
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
                                state.AddError(displayName, ErrorKind.Type, "object {0} has too many anchors (expected compass direction)", displayName);
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
                                state.AddError(displayName, ErrorKind.Type, "object {0} has too many offsets (expected point)", displayName);
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
                            margin = margin with
                            {
                                Left = nma.Left ?? margin.Left,
                                Top = nma.Top ?? margin.Top,
                                Right = nma.Right ?? margin.Right,
                                Bottom = nma.Bottom ?? margin.Bottom
                            };
                            break;

                        case AST.NodeCornerRadiusAttribute ncra:
                            cornerRadius = ncra.Value;
                            break;
                    }
                },
                r => regionConfig = ApplyRegionAttributes(regionConfig, r),
                t => font = ApplyFontAttributes(font, t));
            }

            var objectScope = scope.Chain(node.Name?.AsKey ?? node.Classes.First().AsKey, local);
            var childEntities = new List<IR.Entity>();
            
            foreach (var declaration in node.Classes.SelectMany(c => scope.FindObjectClass(c, true).Children))
            {
                switch (declaration)
                {
                    case AST.TypedClass c:
                        AddClass(c, objectScope);
                        break;

                    case AST.TypedObject o:
                        childEntities.Add(AddObject(o, font, objectScope, false));
                        break;

                    case AST.TypedLine l:
                        var (objects, edges) = AddLine(l, font, objectScope, false);
                        childEntities.AddRange(objects);
                        childEntities.AddRange(edges);
                        break;
                }
            }

            foreach (var declaration in node.Declarations)
            {
                switch (declaration)
                {
                    case AST.TypedClass c:
                        AddClass(c, objectScope);
                        break;

                    case AST.TypedObject o:
                        childEntities.Add(AddObject(o, font, objectScope, true));
                        break;

                    case AST.TypedLine l:
                        var (objects, edges) = AddLine(l, font, objectScope, true);
                        childEntities.AddRange(objects);
                        childEntities.AddRange(edges);
                        break;
                }
            }

            var result = new IR.Node(
                node.Classes.Select(c => "C_" + state.UnmapSpan(c.AsLoc).ToStringValue()).Append(objectScope.UniqueName()).ToArray(),
                displayName,
                new IR.Region(regionConfig, childEntities), 
                shared.Label == null ? null : new IR.StyledText(font, shared.Label, shared.JustifyLabel),
                !shape.HasValue ? null : new Shape(shape.Value, cornerRadius),
                shared.Stroke,
                alignment, margin, width, height, 
                row, column, position, anchor, offset                 
            );

            scope.AddObject(node.Name, result);

            return result;
        }

        private (IEnumerable<IR.Node>, IEnumerable<IR.Edge>) AddLine(AST.TypedLine line, Font cascadeFont, TypedScope scope, bool local)
        {
            var objects = new List<IR.Node>();
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

            var nodes = new List<(ArrowKind? direction, IR.Node? target, Name? name)>();
            foreach (var seg in line.Segments)
            {
                var target = seg.Target.IsT0 ? scope.FindObject(seg.Target.AsT0) : AddObject(seg.Target.AsT1, cascadeFont, scope, local);
                if (seg.Target.IsT1)
                {
                    objects.Add(target!);
                }
                nodes.Add((seg.Direction, target, seg.Target.IsT0 ? seg.Target.AsT0 : target?.Name));
            }

            var classes = line.Classes.Select(c => "C_" + state.UnmapSpan(c.AsLoc).ToStringValue()).ToArray();
            var label = shared.Label == null ? null : new IR.StyledText(font, shared.Label, shared.JustifyLabel);
            for (var i = 0; i < nodes.Count - 1; i++)
            {
                var from = nodes[i];
                var to = nodes[i + 1];
                
                if (from.target != null && from.name != null && to.target != null && to.name != null && from.direction.HasValue)
                {
                    switch (from.direction.Value)
                    {
                        case ArrowKind.Backward:
                            edges.Add(new(
                                classes,
                                new IR.Endpoint(to.name, to.target, null, anchorStart, offsetStart), 
                                new IR.Endpoint(from.name, from.target, MarkerKind.Arrowhead, anchorEnd, offsetEnd), 
                                shared.Stroke, 
                                label
                            ));
                            break;

                        case ArrowKind.Forward:
                            edges.Add(new(
                                classes,
                                new IR.Endpoint(from.name, from.target, null, anchorStart, offsetStart),
                                new IR.Endpoint(to.name, to.target, MarkerKind.Arrowhead, anchorEnd, offsetEnd),
                                shared.Stroke,
                                label
                            ));
                            break;

                        case ArrowKind.Neither:
                            edges.Add(new(
                                classes,
                                new IR.Endpoint(from.name, from.target, null, anchorStart, offsetStart),
                                new IR.Endpoint(to.name, to.target, null, anchorEnd, offsetEnd),
                                shared.Stroke,
                                label
                            ));
                            break;

                        case ArrowKind.Both:
                            edges.Add(new(
                                classes,
                                new IR.Endpoint(from.name, from.target, MarkerKind.Arrowhead, anchorStart, offsetStart),
                                new IR.Endpoint(to.name, to.target, MarkerKind.Arrowhead, anchorEnd, offsetEnd),
                                shared.Stroke,
                                label
                            ));
                            break;
                            
                        default:
                            state.AddError(from.name, ErrorKind.Internal, $"unknown ArrowKind {from.direction.Value} from {0} to {1}", from.name, to.name);
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
                AST.RegionPaddingAttribute rpa => config with { Padding = config.Padding with
                {
                    Left = rpa.Left ?? config.Padding.Left,
                    Top = rpa.Top ?? config.Padding.Top,
                    Right = rpa.Right ?? config.Padding.Right,
                    Bottom = rpa.Bottom ?? config.Padding.Bottom
                } },
                AST.RegionGridFlowAttribute rgfa => config with { GridFlow = rgfa.Kind },
                AST.RegionGridMaxAttribute rgma => config with { GridMax = rgma.Value },
                AST.RegionGridAttribute rga => config with { GridFlow = rga.Flow ?? config.GridFlow, GridMax = rga.Max ?? config.GridMax },
                AST.RegionGutterColumnsAttribute rsca => config with { Gutter = new(rsca.Value, config.Gutter.Rows) },
                AST.RegionGutterRowsAttribute rsra => config with { Gutter = new(config.Gutter.Columns, rsra.Value) },
                AST.RegionGutterAttribute rsa => config with { Gutter = new(rsa.Columns, rsa.Rows) },
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
