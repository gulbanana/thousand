﻿using System.Collections.Generic;
using Thousand.Model;

// Intermediate representation shared between Canonicalise and Compose stages
namespace Thousand.IR
{
    // attributes that always go together - candidates for moving to Model
    public record Text(string Label, int FontSize);
    public record Stroke(Colour Colour, int? Width);

    public record Config(float Scale, Colour Background);
        
    public record Object
    (
        string? Name,
        Text? Text,
        // layout
        int? Row, int? Column, int? Width, int? Height,
        // shape
        ShapeKind Kind, int Padding, int CornerRadius, Stroke Stroke, Colour Fill
    )
    {
        public Object(string name) : this(name, new Text(name, 20), null, null, null, null, ShapeKind.RoundRect, 15, 5, new Stroke(Colour.Black, 1), Colour.White) { }
    }
    
    public record Edge(Object FromTarget, Object ToTarget, Point FromOffset, Point ToOffset, Colour Stroke, float? Width);
    
    public record Rules(Config Config, IReadOnlyList<Object> Objects, IReadOnlyList<Edge> Edges);
}
