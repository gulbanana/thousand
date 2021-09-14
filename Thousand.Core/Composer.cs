using System.Collections.Generic;

namespace Thousand
{
    public static class Composer
    {
        public static Layout.Diagram Compose(AST.Document document)
        {
            var labels = new List<Layout.Label>();

            var nextX = 50;
            foreach (var node in document.Nodes)
            {
                labels.Add(new(nextX, 50, node.Label));
                nextX += 100;
            }

            return new(labels.Count * 100, 100, labels);
        }
    }
}
