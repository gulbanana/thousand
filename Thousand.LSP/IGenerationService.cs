using OmniSharp.Extensions.LanguageServer.Protocol;
using Thousand.Layout;
using Thousand.LSP.Analyse;

namespace Thousand.LSP
{
    public interface IGenerationService
    {
        void Update(DocumentUri key, Diagram diagram);
        void Track(DocumentUri uri);
        void Untrack(DocumentUri uri);
        string? Export(DocumentUri uri, Analysis analysis, bool png);
    }
}
