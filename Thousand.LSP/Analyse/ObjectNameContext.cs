using Superpower.Model;
using System;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Thousand.LSP.Analyse
{
    public record ObjectNameContext(UntypedScope Scope, TextSpan Span)
    {
        private Lazy<Range> location = new Lazy<Range>(() => Span.AsRange());
        public Range Location => location.Value;
    }
}
