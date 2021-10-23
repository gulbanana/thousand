using OmniSharp.Extensions.LanguageServer.Protocol;
using System;
using Thousand.Parse;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Thousand.LSP.Analyse
{
    public sealed class Reference<T>
    {
        public DocumentUri Uri { get; }
        private readonly Lazy<Range> range;
        public Range Range => range.Value;
        public T Value { get; }

        public Reference(DocumentUri uri, ILocated location, T value)
        {
            Uri = uri;
            range = new Lazy<Range>(() => location.Span.AsRange());
            Value = value;
        }

        public Reference(DocumentUri uri, Macro location, T value)
        {
            Uri = uri;
            range = new Lazy<Range>(() => location.Span().AsRange());
            Value = value;
        }

        public void Deconstruct(out DocumentUri u, out Range r, out T v)
        {
            u = Uri;
            r = Range;
            v = Value;
        }
    }
}
