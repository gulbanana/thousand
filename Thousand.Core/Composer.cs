using System.Collections.Generic;

namespace Thousand
{
    public static class Composer
    {
        internal const int W = 150;

        public static Layout.Diagram Compose(AST.Document document)
        {
            var labels = new List<Layout.Label>();

            var nextX = W/2;
            foreach (var node in document.Nodes)
            {
                labels.Add(new(nextX, W/2, node.Label));
                nextX += W;
            }

            return new(labels.Count * W, W, labels);
        }
    }
}
