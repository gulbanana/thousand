using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Thousand.Model;

namespace Thousand
{
    public static class Composer
    {
        internal const int W = 150;

        public static bool TryCompose(AST.Document document, [NotNullWhen(true)] out Layout.Diagram? diagram, out GenerationError[] warnings, out GenerationError[] errors)
        {
            var ws = new List<string>();
            var es = new List<string>();

            var ir = new Canonicalisation(ws, es, document);

            var edges = new List<IR.Edge>();

            foreach (var chain in document.Declarations.OfType<AST.Edges>())
            {
                var stroke = Colour.Black;

                foreach (var attr in chain.Attributes)
                {
                    switch (attr)
                    {
                        case AST.EdgeStrokeAttribute esa:
                            stroke = esa.Colour;
                            break;
                    }
                }

                for (var i = 0; i < chain.Elements.Length - 1; i++)
                {
                    var from = chain.Elements[i];
                    var to = chain.Elements[i+1];

                    var fromTarget = ir.FindObject(from.Target);
                    var toTarget = ir.FindObject(to.Target);

                    if (fromTarget != null && toTarget != null && from.Direction.HasValue)
                    {
                        if (from.Direction.Value == ArrowKind.Forward)
                        {
                            edges.Add(new(fromTarget, toTarget, stroke));
                        }
                        else
                        {
                            edges.Add(new(toTarget, fromTarget, stroke));
                        }
                    }                    
                }
            }

            // pass 2: create drawables from the canonicalised AST
            var shapes = new Dictionary<IR.Object, Layout.Shape>();
            var labels = new List<Layout.Label>();
            var lines = new List<Layout.Line>();

            foreach (var obj in ir.Objects)
            {
                if (obj.Label != null)
                {
                    var label = new Layout.Label(obj.Column * W - (W / 2), obj.Row * W - (W / 2), obj.Label, obj.FontSize);
                    var shape = new Layout.Shape(obj.Name, label.X, label.Y, obj.Kind, label, obj.Stroke, obj.Fill);

                    labels.Add(label);
                    shapes.Add(obj, shape);
                }
                else
                {
                    var shape = new Layout.Shape(obj.Name, obj.Column * W - (W / 2), obj.Row * W - (W / 2), obj.Kind, null, obj.Stroke, obj.Fill);

                    shapes.Add(obj, shape);
                }
            }

            foreach (var edge in edges)
            {
                var from = shapes[edge.FromTarget];
                var to = shapes[edge.ToTarget];

                lines.Add(new(from, to, edge.Stroke));
            }

            // pass 3: apply doc-level attributes
            var scale = 1f;
            var background = Colour.White;            

            foreach (var attr in document.Declarations.OfType<AST.DocumentAttribute>())
            {
                switch (attr)
                {
                    case AST.DocumentScaleAttribute dsa:
                        scale = dsa.Value;
                        break;

                    case AST.DocumentBackgroundAttribute dba:
                        background = dba.Colour;
                        break;
                }
            }

            warnings = ws.Select(w => new GenerationError(w)).ToArray();
            errors = es.Select(w => new GenerationError(w)).ToArray();
            diagram = new(
                ir.Objects.Select(s => s.Column).Append(0).Max() * W,
                ir.Objects.Select(s => s.Row).Append(0).Max() * W, 
                scale,
                background,
                shapes.Values.ToList(), 
                labels, 
                lines
            );

            return !es.Any();
        }
    }
}
