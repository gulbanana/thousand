using Superpower.Model;
using Thousand.AST;

namespace Thousand.LSP.Analyse
{
    public class AttributeContext
    {
        public UntypedAttribute Syntax { get; }
        public ParentKind ParentKind { get; }
        public string[] ParentAttributes { get; }
        public TextSpan KeySpan { get; }

        public AttributeContext(UntypedAttribute syntax, ParentKind parentKind, string[] parentAttributes, TextSpan endSpan)
        {
            Syntax = syntax;
            ParentKind = parentKind;
            ParentAttributes = parentAttributes;
            KeySpan = syntax.Key?.Span ?? syntax.Value.Span(endSpan);
        }
    }
}
