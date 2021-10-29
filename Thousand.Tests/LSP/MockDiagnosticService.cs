using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Thousand.LSP;

namespace Thousand.Tests.LSP
{
    class MockDiagnosticService : IDiagnosticService
    {
        public GenerationState LastState = new GenerationState();

        public bool HasDiagnostics(DocumentUri uri, Position position) => false;
        public void Track(DocumentUri uri) { }
        public void Untrack(DocumentUri uri) { }
        public void Update(DocumentUri uri, GenerationState state, string source) => LastState = state;
    }
}
