using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Thousand.Parse;

namespace Thousand.LSP.Analyse
{
    public struct ObjectNameContext
    {
        public Range Range { get; }
        public UntypedScope Scope { get; }
        public string Text { get; }

        public ObjectNameContext(UntypedScope scope, Identifier identifier)
        {
            Scope = scope;
            Text = identifier.Text;
            Range = identifier.Span.AsRange();
        }
    }
}
