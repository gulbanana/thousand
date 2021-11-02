using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Superpower.Model;
using Thousand.AST;

namespace Thousand.LSP.Analyse
{
    public struct AttributeContext
    {
        public Range Range { get; }
        public UntypedAttribute Syntax { get; }
        public ParentKind ParentKind { get; }
        public string[] ParentAttributes { get; }

        public AttributeContext(UntypedAttribute syntax, ParentKind parentKind, string[] parentAttributes, TextSpan endSpan)
        {
            Syntax = syntax;
            ParentKind = parentKind;
            ParentAttributes = parentAttributes;
            Range = syntax.Key?.AsLoc.AsRange() ?? syntax.Value.AsRange(endSpan);
        }
    }
}
