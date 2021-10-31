using Thousand.Parse;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Thousand.LSP.Analyse
{
    public struct ClassNameContext
    {
        public Range Range { get; }
        public UntypedScope Scope { get; }
        public string Text { get; }
        public bool IsAtStart { get; }

        public ClassNameContext(UntypedScope scope, bool isAtStart, Identifier identifier)
        {
            Scope = scope;
            IsAtStart = isAtStart;
            Text = identifier.Text;
            Range = identifier.Span.AsRange();
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
