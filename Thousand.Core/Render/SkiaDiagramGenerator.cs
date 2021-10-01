using SkiaSharp;

namespace Thousand.Render
{
    public class SkiaDiagramGenerator : DiagramGenerator<SKImage>
    {
        public SkiaDiagramGenerator() : base(new SkiaRenderer()) { }
    }
}
