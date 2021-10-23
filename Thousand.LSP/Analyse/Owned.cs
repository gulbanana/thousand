using OmniSharp.Extensions.LanguageServer.Protocol;

namespace Thousand.LSP.Analyse
{
    public class Owned<T>
    {
        public DocumentUri Uri { get; }
        public T Value { get; }

        public Owned(DocumentUri uri, T value)
        {
            Uri = uri;
            Value = value;
        }

        public void Deconstruct(out DocumentUri u, out T v)
        {
            u = Uri;
            v = Value;
        }
    }
}
