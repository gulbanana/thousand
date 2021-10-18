using OmniSharp.Extensions.LanguageServer.Protocol;
using Thousand.Layout;

namespace Thousand.LSP
{
    public interface IGenerationService
    {
        void Update(DocumentUri key, Diagram diagram);
        void Track(DocumentUri uri);
        void Untrack(DocumentUri uri);
    }
}
