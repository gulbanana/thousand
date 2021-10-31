using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Thousand.Parse;

namespace Thousand.LSP.Analyse
{
    public struct Located<T>
    {
        public DocumentUri Uri { get; }
        public T Value { get; }
        public Range Range { get; }

        public Located(DocumentUri uri, T value, ILocated location)
        {
            Uri = uri;
            Value = value;
            Range = location.Span.AsRange();
        }

        public Located(ParsedDocument doc, T value, IMacro location)
        {
            Uri = doc.Uri;
            Value = value;
            Range = location.AsRange(doc.EndSpan);
        }

        public void Deconstruct(out DocumentUri u, out Range r, out T v)
        {
            u = Uri;
            r = Range;
            v = Value;
        }
    }
}
