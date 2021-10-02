using SkiaSharp;
using System;
using System.Collections.Generic;
using Thousand.Model;
using Topten.RichTextKit;

namespace Thousand.Render
{
    internal class SkiaRenderState
    {
        public readonly Dictionary<Layout.Shape, SKPath> ShapePaths;

        public SkiaRenderState()
        {
            ShapePaths = new();
        }

        public void PaintDiagram(SKCanvas canvas, Layout.Diagram diagram)
        {
            canvas.Scale(diagram.Scale);
            canvas.Clear(diagram.Background.SK());
        }

        public void PaintLabel(SKCanvas canvas, Layout.Label label)
        {
            var text = new RichString()
                .FontFamily(label.Font.Family)
                .FontSize(label.Font.Size)
                .Alignment(TextAlignment.Center)
                .Add(label.Content);

            text.Paint(canvas, label.Bounds.Origin.SK());
        }

        public void PaintShape(SKCanvas canvas, Layout.Shape shape)
        {
            var path = ShapePaths[shape];
            
            var fill = new SKPaint { Color = shape.Fill.SK(), IsAntialias = true };
            canvas.DrawPath(path, fill);

            var stroke = new SKPaint { Color = shape.Stroke.Colour.SK(), IsAntialias = true, IsStroke = true, PathEffect = StrokeEffect(shape.Stroke.Style) };
            if (shape.Stroke.Width is not ZeroWidth)
            {
                stroke.StrokeWidth = shape.Stroke.Width switch
                {
                    HairlineWidth => 0,
                    PositiveWidth(var w) => w,
                    _ => throw new NotSupportedException()
                };

                canvas.DrawPath(path, stroke);
            }                        
        }

        public void PaintLine(SKCanvas canvas, Layout.Line line)
        {
            var start = line.Start.SK();
            var end = line.End.SK();

            var stroke = new SKPaint { Color = line.Stroke.Colour.SK(), IsAntialias = true, PathEffect = StrokeEffect(line.Stroke.Style) };
            var fill = new SKPaint { Color = line.Stroke.Colour.SK(), IsAntialias = true };

            // draw the main line
            if (line.Stroke.Width is not ZeroWidth)
            {
                stroke.StrokeWidth = line.Stroke.Width switch
                {
                    HairlineWidth => 0,
                    PositiveWidth(var w) => w,
                    _ => throw new NotSupportedException()
                };

                canvas.DrawLine(start, end, stroke);
            }

            // draw end cap
            var arrowhead = new SKPath();
            var arrowLength = (end - start).Normalize(7f);
            var arrowWidth = (end - start).Normalize(4f);
            var base1 = SKMatrix.CreateRotationDegrees(-90).MapPoint(arrowWidth);
            var base2 = SKMatrix.CreateRotationDegrees(90).MapPoint(arrowWidth);

            arrowhead.MoveTo(end - arrowLength);
            arrowhead.LineTo(end - arrowLength + base1);
            arrowhead.LineTo(end);
            arrowhead.LineTo(end - arrowLength + base2);
            arrowhead.Close();

            canvas.DrawPath(arrowhead, fill);
        }

        private static SKPathEffect? StrokeEffect(StrokeKind style) => style switch
        {
            StrokeKind.Dashed => SKPathEffect.CreateDash(new[] { 3f, 2f }, 0f),
            _ => null
        };
    }
}
