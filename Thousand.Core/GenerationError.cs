using Superpower.Model;
using System;

namespace Thousand
{
    public record GenerationError(TextSpan Span, ErrorKind Kind, string Message, string? Details = null)
    {
        public GenerationError(Exception e) : this(new TextSpan(), ErrorKind.Internal, e.Message, e.ToString()) { }

        public override string ToString()
        {
            if (Span.Position.HasValue)
            {
                return $"{Kind} error (line {Span.Position.Line}, column {Span.Position.Column}): {Message}.";
            }
            else
            {
                return $"{Kind} error: {Message}.";
            }
        }
    }
}
