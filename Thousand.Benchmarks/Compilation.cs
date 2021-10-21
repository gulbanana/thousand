using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Logging.Abstractions;
using OmniSharp.Extensions.LanguageServer.Protocol;
using System.IO;
using Thousand.LSP;
using Thousand.LSP.Analyse;

namespace Thousand.Benchmarks
{
    public class Compilation
    {
        [Params("connectors.1000", "tetris.1000", "underground.1000")]
        public string Input { get; set; } = default!;

        private string source;
        private DocumentUri key;
        private readonly DiagramGenerator generator;
        private readonly BufferService bufferService;
        private readonly AnalysisService analysisService;

        public Compilation()
        {
            generator = new DiagramGenerator();
            bufferService = new BufferService();
            analysisService = new AnalysisService(NullLogger<AnalysisService>.Instance, bufferService, new MockDiagnosticService(), new MockGenerationService());
        }

        [GlobalSetup]
        public void Setup()
        {
            source = File.ReadAllText(Input);
            key = new DocumentUri(null, null, "/" + Input, null, null, false);
            bufferService.Add(key, source);
        }

        [Benchmark]
        public Layout.Diagram Batch()
        {
            return generator.GenerateDiagram(source).AsT0.Diagram;
        }

        [Benchmark]
        public Analysis Interactive()
        {
            return analysisService.Analyse(key);
        }
    }
}
