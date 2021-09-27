using System;
using System.Collections.Generic;
using Thousand.Model;

namespace Thousand
{
    // a fig-leaf for Skia
    public interface IRenderer<T> : IDisposable
    {
        IReadOnlyDictionary<string, Point> MeasureTextBlocks(IR.Rules ir);
        T Render(Layout.Diagram diagram);
    }
}
