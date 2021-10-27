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
        private readonly API.Metadata api;
        private readonly DiagramGenerator generator;
        private readonly BufferService bufferService;
        private readonly AnalysisService analysisService;
        private readonly ServerOptions options;

        public Compilation()
        {
            api = new API.Metadata();
            generator = new DiagramGenerator();
            bufferService = new BufferService();
            analysisService = new AnalysisService(NullLogger<AnalysisService>.Instance, api, bufferService, new MockDiagnosticService(), new MockGenerationService());
            options = new ServerOptions();
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
            return analysisService.Analyse(options, key: key);
        }
    }
}
