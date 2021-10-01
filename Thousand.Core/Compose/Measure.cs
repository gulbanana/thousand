using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using Thousand.Model;

namespace Thousand.Compose
{
    public static class Measure
    {
        public static Point TextBlock(IR.Text t)
        {
            var text = new Topten.RichTextKit.RichString()
                .FontFamily(SKTypeface.Default.FamilyName)
                .FontSize(t.FontSize)
                .Alignment(Topten.RichTextKit.TextAlignment.Center)
                .Add(t.Label);

            return new((int)MathF.Ceiling(text.MeasuredWidth), (int)MathF.Ceiling(text.MeasuredHeight));
        }

        public static IReadOnlyDictionary<string, Point> TextBlocks(IR.Rules ir)
        {
            var result = new Dictionary<string, Point>();

            foreach (var t in ir.Objects.Select(o => o.Text).WhereNotNull())
            {                
                var text = new Topten.RichTextKit.RichString()
                    .FontFamily(SKTypeface.Default.FamilyName)
                    .FontSize(t.FontSize)
                    .Alignment(Topten.RichTextKit.TextAlignment.Center)
                    .Add(t.Label);

                result[t.Label] = new((int)MathF.Ceiling(text.MeasuredWidth), (int)MathF.Ceiling(text.MeasuredHeight));
            }

            return result;
        }
    }
}
