using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Superpower.Model;
using System.Collections.Generic;
using Thousand.AST;

namespace Thousand.LSP.Analyse
{
    // XXX add uint version, might help to avoid races
    public record ParsedDocument(DocumentUri Uri, TextSpan EndSpan, UntypedDocument Syntax)
    {
        public List<DocumentSymbol> Symbols { get; } = new();
        public List<AttributeContext> Attributes { get; } = new();
        public List<ClassNameContext> ClassNames { get; } = new();
        public List<ObjectNameContext> ObjectNames { get; } = new();
    }
}
