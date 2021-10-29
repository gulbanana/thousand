using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Thousand.LSP
{
    public interface IDiagnosticService
    {
        public bool HasDiagnostics(DocumentUri uri, Position position);
        public void Track(DocumentUri uri);
        public void Update(DocumentUri uri, GenerationState state, string source);
        public void Untrack(DocumentUri uri);
    }
}
