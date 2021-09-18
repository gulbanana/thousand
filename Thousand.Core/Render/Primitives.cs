using SkiaSharp;
using Topten.RichTextKit;

namespace Thousand.Render
{
    internal record PLabel(RichString Text, SKPoint Center, SKPoint Origin);
    internal record PShape(SKPoint Center, SKPath Path);
}
