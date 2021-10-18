using OmniSharp.Extensions.LanguageServer.Protocol;
using Thousand.Layout;
using Thousand.LSP;

namespace Thousand.Tests.LSP
{
    class MockGenerationService : IGenerationService
    {
        public void Track(DocumentUri uri) { }
        public void Untrack(DocumentUri uri) { }
        public void Update(DocumentUri key, Diagram diagram) { }
    }
}