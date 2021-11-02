using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Thousand.Model;

namespace Thousand.LSP.Analyse
{
    public struct ObjectNameContext
    {
        public Range Range { get; }
        public UntypedScope Scope { get; }
        public string Text { get; }

        public ObjectNameContext(UntypedScope scope, Name identifier)
        {
            Scope = scope;
            Text = identifier.AsKey;
            Range = identifier.AsLoc.AsRange();
        }
    }
}
