using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Thousand
{
    public static class Composer
    {
        internal const int W = 150;

        public static bool TryCompose(AST.Document document, [NotNullWhen(true)] out Layout.Diagram? diagram, out GenerationError[] warnings, out GenerationError[] errors)
        {
            return TryCompose(new[] { document }, out diagram, out warnings, out errors);
        }

        public static bool TryCompose(IEnumerable<AST.Document> documents, [NotNullWhen(true)] out Layout.Diagram? diagram, out GenerationError[] warnings, out GenerationError[] errors)
        {
            var ws = new List<string>();
            var es = new List<string>();

            var ir = new Canonicalisation(ws, es, documents);

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

            foreach (var edge in ir.Edges)
            {
                var from = shapes[edge.FromTarget];
                var to = shapes[edge.ToTarget];

                lines.Add(new(from, to, edge.Stroke, edge.Width));
            }

            warnings = ws.Select(w => new GenerationError(w)).ToArray();
            errors = es.Select(w => new GenerationError(w)).ToArray();
            diagram = new(
                ir.Objects.Select(s => s.Column).Append(0).Max() * W,
                ir.Objects.Select(s => s.Row).Append(0).Max() * W, 
                ir.Config.Scale,
                ir.Config.Background,
                shapes.Values.ToList(), 
                labels, 
                lines
            );

            return !es.Any();
        }
    }
}
