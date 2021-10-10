using Superpower.Model;
using System;

namespace Thousand
{
    public record GenerationError(Position Position, int Length, ErrorKind Kind, string Message, string? Details = null)
    {
        public GenerationError(TextSpan span, ErrorKind Kind, string Message, string? Details = null) : this(span.Position, span.Length, Kind, Message, Details) { }

        public GenerationError(Exception e) : this(Position.Empty, 0, ErrorKind.Internal, e.Message, e.ToString()) { }

        public override string ToString()
        {
            if (Position.HasValue)
            {
                return $"{Kind} error (line {Position.Line}, column {Position.Column}): {Message}.";
            }
            else
            {
                return $"{Kind} error: {Message}.";
            }
        }
    }
}
