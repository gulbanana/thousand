using Superpower.Model;
using System;

namespace Thousand
{
    public record GenerationError(Position Position, ErrorKind Kind, string Message, string? Details = null)
    {
        public GenerationError(Exception e) : this(Position.Empty, ErrorKind.Internal, e.Message, e.ToString()) { }

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
