using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Superpower.Model;
using System.Linq;
using Thousand.Parse;

namespace Thousand.LSP
{
    static class BufferExtensions
    {
        public static Range AsRange(this TextSpan span)
        {
            var start = span.Position;
            var end = span.Skip(span.Length).Position;
            return new(
                start.Line - 1,
                start.Column - 1,
                end.Line - 1,
                end.Column - 1
            );
        }

        public static Range AsRange(this IMacro macro, TextSpan endSpan)
        {
            if (macro.Location.IsAtEnd)
            {
                return new Range(endSpan.Position.Line - 1, endSpan.Position.Column - 1, endSpan.Position.Line - 1, endSpan.Position.Column - 1);
            }

            var first = macro.Location.First().Span;
            if (macro.Remainder.IsAtEnd)
            {
                return new Range(first.Position.Line - 1, first.Position.Column - 1, endSpan.Position.Line - 1, endSpan.Position.Column - 1);
            }
            else
            {
                var tokenDiff = macro.Remainder.Position - macro.Location.Position;
                if (tokenDiff == 0)
                {
                    return new Range(first.Position.Line - 1, first.Position.Column - 1, first.Position.Line - 1, first.Position.Column - 1);
                }
                else
                {
                    var last = macro.Location.ElementAt(tokenDiff - 1).Span;
                    if (last.Length > 0) last = last.Skip(last.Length);
                    return new Range(first.Position.Line - 1, first.Position.Column - 1, last.Position.Line - 1, last.Position.Column - 1);
                }
            }
        }
    }
}
