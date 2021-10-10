using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
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
            canvas.Scale((float)diagram.Scale);
            canvas.Clear(diagram.Background.SK());
        }

        // this could draw line-by-line, but the results seem to be identical
        public void PaintLabel(SKCanvas canvas, Layout.LabelBlock block)
        {
            var text = new RichString()
                .FontFamily(block.Font.Family)
                .FontSize(block.Font.Size)
                .TextColor(block.Font.Colour.SK())
                .Alignment(TextAlignment.Center)
                .Add(block.Content);

            text.Paint(canvas, block.Bounds.Origin.SK());
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
                    _ => throw new NotSupportedException($"unknown shape `{shape.Kind}`")
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
                    _ => throw new NotSupportedException($"unknown width `{line.Stroke.Width}`")
                };

                canvas.DrawLine(start, end, stroke);
            }

            // draw markers
            if (line.StartMarker)
            {
                var arrowhead = CreateArrowhead(end, start);
                canvas.DrawPath(arrowhead, fill);
            }

            if (line.EndMarker)
            {
                var arrowhead = CreateArrowhead(start, end);
                canvas.DrawPath(arrowhead, fill);
            }
        }

        private SKPath CreateArrowhead(SKPoint start, SKPoint end)
        {
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

            return arrowhead;
        }

        private static SKPathEffect? StrokeEffect(StrokeKind style) => style switch
        {
            StrokeKind.Solid => null,
            _ => SKPathEffect.CreateDash(Shapes.Dashes(style).Select(x => (float)x).ToArray(), 0f)
        };
    }
}
