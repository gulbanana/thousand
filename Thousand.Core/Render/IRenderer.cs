using System;

namespace Thousand.Render
{
    // a fig-leaf for Skia
    public interface IRenderer<T> : IDisposable
    {
        T Render(Layout.Diagram diagram);
    }
}
