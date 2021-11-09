using MediatR;
using Microsoft.Extensions.Configuration;
using OmniSharp.Extensions.JsonRpc;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using System.Threading;
using System.Threading.Tasks;
using Thousand.LSP.Analyse;

namespace Thousand.LSP.Extensions
{
    class PreviewHandler : IJsonRpcNotificationHandler<BeginPreview>, IJsonRpcNotificationHandler<EndPreview>, IJsonRpcRequestHandler<ExportImageRequest, ExportImageResult>
    {
        private readonly ILanguageServerConfiguration configuration;
        private readonly IGenerationService generationService;
        private readonly AnalysisService analysisService;

        public PreviewHandler(ILanguageServerConfiguration configuration, IGenerationService generationService, AnalysisService analysisService)
        {
            this.configuration = configuration;
            this.generationService = generationService;
            this.analysisService = analysisService;
        }

        public async Task<Unit> Handle(BeginPreview request, CancellationToken cancellationToken)
        {
            var config = await configuration.GetScopedConfiguration(request.Uri, cancellationToken);
            var options = new ServerOptions();
            config.GetSection("thousand").GetSection("server").Bind(options);

            generationService.Track(request.Uri);
            analysisService.Reparse(request.Uri, options);

            return Unit.Value;
        }

        public Task<Unit> Handle(EndPreview request, CancellationToken cancellationToken)
        {
            generationService.Untrack(request.Uri);

            return Task.FromResult(Unit.Value);
        }

        public async Task<ExportImageResult> Handle(ExportImageRequest request, CancellationToken cancellationToken)
        {
            var analysis = await analysisService.GetAnalysisAsync(request.Uri);
            return new ExportImageResult { Filename = await Task.Run(() => generationService.Export(request.Uri, analysis, request.Format == "png")) };
        }
    }
}
