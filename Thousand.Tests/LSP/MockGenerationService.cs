using OmniSharp.Extensions.LanguageServer.Protocol;
using Thousand.Layout;
using Thousand.LSP;
using Thousand.LSP.Analyse;

namespace Thousand.Tests.LSP
{
    class MockGenerationService : IGenerationService
    {
        public void Track(DocumentUri uri) { }
        public void Untrack(DocumentUri uri) { }
        public void Update(DocumentUri key, Diagram diagram) { }
        public string? Export(DocumentUri uri, Analysis analysis, bool png) => null;
    }
}