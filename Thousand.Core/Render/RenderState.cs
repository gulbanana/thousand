using SkiaSharp;
using System.Collections.Generic;

namespace Thousand.Render
{
    internal class RenderState
    {
        public readonly SKCanvas Canvas;
        public readonly Dictionary<Layout.Label, PLabel> Labels;
        public readonly Dictionary<Layout.Shape, PShape> Shapes;

        public RenderState(SKCanvas canvas)
        {
            Canvas = canvas;
            Labels = new();
            Shapes = new();
        }
    }
}
