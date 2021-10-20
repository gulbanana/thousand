using System;
using Thousand.Parse;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Thousand.LSP.Analyse
{
    public sealed class Reference<T>
    {
        private Lazy<Range> range;
        public Range Range => range.Value;
        public T Value { get; }

        public Reference(ILocated location, T value)
        {
            range = new Lazy<Range>(() => location.Span.AsRange());
            Value = value;
        }

        public Reference(Macro location, T value)
        {
            range = new Lazy<Range>(() => location.Span().AsRange());
            Value = value;
        }

        public void Deconstruct(out Range r, out T v)
        {
            r = Range;
            v = Value;
        }
    }
}
