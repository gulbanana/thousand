using System.Collections.Generic;

namespace Thousand.Layout
{
    public record Label(int X, int Y, string Content);
    public record Diagram(int Width, int Height, IReadOnlyList<Label> Labels);
}
