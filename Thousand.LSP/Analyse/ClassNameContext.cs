using Thousand.Model;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Thousand.LSP.Analyse
{
    public struct ClassNameContext
    {
        public Range Range { get; }
        public UntypedScope Scope { get; }
        public string Text { get; }
        public bool IsAtStart { get; }

        public ClassNameContext(UntypedScope scope, bool isAtStart, Name identifier)
        {
            Scope = scope;
            IsAtStart = isAtStart;
            Text = identifier.AsKey;
            Range = identifier.AsLoc.AsRange();
        }

        public ClassNameContext(UntypedScope scope, bool isAtStart, Range range)
        {
            Scope = scope;
            IsAtStart = isAtStart;
            Text = string.Empty;
            Range = range;
        }
    }
}
