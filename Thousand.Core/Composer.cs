﻿using System;
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
            var lines = new List<Layout.Line>();

            var indexedNodes = document.Declarations.OfType<AST.Node>().ToList();
            var indexedPoints = new (int x, int y)[indexedNodes.Count];

            var nextX = W/2;
            for (var i = 0; i < indexedNodes.Count; i++)
            {
                var node = indexedNodes[i];
                var point = (x: nextX, y: W / 2);
                indexedPoints[i] = point;

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

                var layoutLabel = new Layout.Label(point.x, point.y, label);

                labels.Add(layoutLabel);
                shapes.Add(new(point.x, point.y, shape, layoutLabel));

                nextX += W;
            }

            foreach (var edge in document.Declarations.OfType<AST.Edge>())
            {
                var nFrom = indexedNodes.Single(n => n.Label.Equals(edge.From, StringComparison.OrdinalIgnoreCase));
                var nTo = indexedNodes.Single(n => n.Label.Equals(edge.To, StringComparison.OrdinalIgnoreCase));

                var from = indexedNodes.IndexOf(nFrom);               
                var to = indexedNodes.IndexOf(nTo);

                lines.Add(new(indexedPoints[from].x, indexedPoints[from].y, indexedPoints[to].x, indexedPoints[to].y));
            }

            return new(labels.Count * W, W, shapes, labels, lines);
        }
    }
}
