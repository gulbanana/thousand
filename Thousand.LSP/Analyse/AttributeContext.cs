using System;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Thousand.LSP.Analyse
{
    public record AttributeContext(AST.UntypedAttribute Syntax, ParentKind ParentKind, string[] ParentAttributes)
    {
        private Lazy<Range> keyLocation = new Lazy<Range>(() => Syntax.Key.Span.AsRange());
        public Range KeyLocation => keyLocation.Value;
    }
}
