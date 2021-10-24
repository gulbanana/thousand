using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System.Collections.Generic;
using Thousand.AST;

namespace Thousand.LSP.Analyse
{
    // XXX add uint version, might help to avoid races
    public record ParsedDocument(DocumentUri Uri, string Source, UntypedDocument Syntax)
    {
        public List<UntypedAttribute> Attributes { get; } = new();
        public List<DocumentSymbol> Symbols { get; } = new();
    }
}
