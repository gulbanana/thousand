using System;

namespace Thousand
{
    public record GenerationError(string Message)
    {
        public GenerationError(Exception e) : this(e.Message) { }
    }
}
