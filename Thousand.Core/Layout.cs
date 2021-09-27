﻿using System.Collections.Generic;
using Thousand.Model;

// Intermediate representation shared between Compose and Render stages
namespace Thousand.Layout
{
    public record Label(Rect Bounds, string Content, float FontSize);
    public record Shape(string? Name, ShapeKind Kind, Rect Bounds, float? StrokeWidth, Colour Stroke, Colour Fill); // XXX should not have Name
    public record Line(Shape From, Shape To, Point Start, Point End, Colour Stroke, float? Width);
    public record Diagram(int Width, int Height, float Scale, Colour Background, IReadOnlyList<Shape> Shapes, IReadOnlyList<Label> Labels, IReadOnlyList<Line> Lines);
}
