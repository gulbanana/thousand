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
        private readonly SKCanvas canvas;

        public SkiaRenderState(SKCanvas canvas)
        {
            this.canvas = canvas;
        }

        public void ProcessCommandList(IReadOnlyList<Layout.Command> commands)
        {
            foreach (var command in commands)
            {
                switch (command)
                {
                    case Layout.Drawing shape:
                        PaintShape(shape);
                        break;

                    case Layout.Line line:
                        PaintLine(line);
                        break;

                    case Layout.Label label:
                        PaintLabel(label);
                        break;

                    case Layout.Transform transform:
                        var saveMatrix = canvas.TotalMatrix;
                        if (transform.Scale.HasValue)
                        {
                            canvas.Scale((float)transform.Scale.Value);
                        }

                        ProcessCommandList(transform.Commands);

                        canvas.SetMatrix(saveMatrix);
                        break;
                }
            }
        }

        private void PaintShape(Layout.Drawing shape)
        {
            var path = SkiaPath.Create(shape);

            var fill = new SKPaint { Color = shape.Fill.SK(), IsAntialias = true };
            canvas.DrawPath(path, fill);

            var stroke = new SKPaint { Color = shape.Stroke.Colour.SK(), IsAntialias = true, IsStroke = true, PathEffect = StrokeEffect(shape.Stroke.Style) };
            if (shape.Stroke.Width is not NoWidth)
            {
                stroke.StrokeWidth = shape.Stroke.Width switch
                {
                    HairlineWidth => 0,
                    PositiveWidth(var w) => w,
                    _ => throw new NotSupportedException($"unknown width `{shape.Stroke.Width}`")
                };

                canvas.DrawPath(path, stroke);
            }
        }

        private void PaintLine(Layout.Line line)
        {
            var start = line.Start.SK();
            var end = line.End.SK();

            var stroke = new SKPaint { Color = line.Stroke.Colour.SK(), IsAntialias = true, PathEffect = StrokeEffect(line.Stroke.Style) };
            var fill = new SKPaint { Color = line.Stroke.Colour.SK(), IsAntialias = true };

            // draw the main line
            if (line.Stroke.Width is not NoWidth)
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

        private void PaintLabel(Layout.Label block)
        {
            foreach (var line in block.Lines)
            {
                var text = new RichString()
                    .FontFamily(block.Font.Family)
                    .FontSize((float)block.Font.Size)
                    .TextColor(block.Font.Colour.SK())
                    .Add(line.Content);

                text.Paint(canvas, line.Bounds.Origin.SK());
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
