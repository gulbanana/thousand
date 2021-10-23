using OmniSharp.Extensions.LanguageServer.Protocol;

namespace Thousand.LSP.Analyse
{
    // XXX add uint version, might help to avoid races
    public record ParsedDocument(DocumentUri Uri, string Source, AST.UntypedDocument Syntax);
}
