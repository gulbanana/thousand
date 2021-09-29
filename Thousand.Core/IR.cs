﻿using System.Collections.Generic;
using Thousand.Model;

// Intermediate representation shared between Canonicalise and Compose stages
namespace Thousand.IR
{
    // attributes that always go together - candidates for moving to Model
    public record Text(string Label, int FontSize);
    public record Region(LayoutKind Layout, int Margin, int Gutter)
    {
        public Region() : this(LayoutKind.Grid, 0, 20) { }
    }

    public record Config(float Scale, Colour Background, Region Region)
    {
        public Config() : this(1, Colour.White, new Region()) { }
    }
        
    public record Object
    (
        string? Name,
        Text? Text,
        Region Region,
        // layout
        int? Row, int? Column, int? Width, int? Height,
        // shape
        ShapeKind Kind, int Padding, int CornerRadius, Stroke Stroke, Colour Fill
    )
    {
        public Object(string name) : this(name, new Text(name, 20), new Region(), null, null, null, null, ShapeKind.RoundRect, 15, 5, new Stroke(), Colour.White) { }
    }
    
    public record Edge(Object FromTarget, Object ToTarget, Point FromOffset, Point ToOffset, Stroke Stroke);
    
    public record Rules(Config Config, IReadOnlyList<Object> Objects, IReadOnlyList<Edge> Edges);
}
