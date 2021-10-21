using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Thousand.LSP;

namespace Thousand.Benchmarks
{
    class MockDiagnosticService : IDiagnosticService
    {
        public bool HasDiagnostics(DocumentUri uri, Position position) => false;
        public void Track(DocumentUri uri) { }
        public void Untrack(DocumentUri uri) { }
        public void Update(DocumentUri uri, GenerationState state) { }
    }
}
