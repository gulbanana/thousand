using System.Collections.Generic;

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
                var label = node.Label;
                var shape = ShapeKind.Square;

                foreach (var attr in node.Attributes)
                {
                    switch (attr)
                    {
                        case AST.NodeLabelAttribute nla:
                            label = nla.Content;
                            break;

                        case AST.NodeShapeAttribute nsa:
                            shape = nsa.Kind;
                            break;
                    }
                }

                var layoutLabel = new Layout.Label(nextX, W / 2, label);

                labels.Add(layoutLabel);
                shapes.Add(new(nextX, W / 2, shape, layoutLabel));

                nextX += W;
            }

            return new(labels.Count * W, W, shapes, labels);
        }
    }
}
