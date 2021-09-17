using System;
using System.Collections.Generic;
using System.Linq;

namespace Thousand
{
    public static class Composer
    {
        internal const int W = 150;

        public static Layout.Diagram Compose(AST.Document document)
        {
            var shapes = new List<Layout.Shape>();
            var labels = new List<Layout.Label>();

            var nextX = W/2;
            foreach (var node in document.Nodes)
            {
                var attrs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var attr in node.Attributes)
                {
                    attrs[attr.Key] = attr.Value;
                }

                var shape = attrs.ContainsKey("shape") ? Enum.Parse<ShapeKind>(attrs["shape"], true) : ShapeKind.Square;

                var label = new Layout.Label(nextX, W / 2, node.Label);

                labels.Add(label);
                shapes.Add(new(nextX, W / 2, shape, label));

                nextX += W;
            }

            return new(labels.Count * W, W, shapes, labels);
        }
    }
}
