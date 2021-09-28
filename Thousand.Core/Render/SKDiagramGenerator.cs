using SkiaSharp;

namespace Thousand.Render
{
    public class SKDiagramGenerator : DiagramGenerator<SKImage>
    {
        public SKDiagramGenerator() : base(new Renderer()) { }
    }
}
