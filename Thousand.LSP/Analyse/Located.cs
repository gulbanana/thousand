using OmniSharp.Extensions.LanguageServer.Protocol;
using System;
using Thousand.Parse;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Thousand.LSP.Analyse
{
    public sealed class Located<T> : Owned<T>
    {
        private readonly Lazy<Range> range;
        public Range Range => range.Value;

        public Located(DocumentUri uri, T value, ILocated location) : base(uri, value)
        {
            range = new Lazy<Range>(() => location.Span.AsRange());
        }

        public Located(ParsedDocument doc, T value, Macro location) : base(doc.Uri, value)
        {
            range = new Lazy<Range>(() => location.Span(doc.EndSpan).AsRange());
        }

        public void Deconstruct(out DocumentUri u, out Range r, out T v)
        {
            u = Uri;
            r = Range;
            v = Value;
        }
    }
}
