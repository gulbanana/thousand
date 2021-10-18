using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Thousand.Layout;
using Thousand.LSP.Protocol;
using Thousand.Render;

namespace Thousand.LSP
{
    class GenerationService : IGenerationService
    {
        private readonly string tempDir;
        private readonly HashSet<DocumentUri> current;
        private readonly SkiaRenderer renderer;
        private readonly ILogger<GenerationService> logger;
        private readonly ILanguageServerFacade facade;

        public GenerationService(ILogger<GenerationService> logger, ILanguageServerFacade facade)
        {
            var tempPath = Path.GetTempPath();
            tempDir = Path.Combine(tempPath, "Thousand.LSP");
            Directory.CreateDirectory(tempDir);

            current = new HashSet<DocumentUri>();
            renderer = new SkiaRenderer();

            this.logger = logger;
            this.facade = facade;
        }

        public void Track(DocumentUri uri)
        {
            current.Add(uri);
        }

        public void Untrack(DocumentUri uri)
        {
            current.Remove(uri);
        }

        public void Update(DocumentUri key, Diagram diagram)
        {
            if (!current.Contains(key))
            {
                return;
            }

            _ = Task.Run(() => UpdateImpl(key, diagram));
        }

        private void UpdateImpl(DocumentUri key, Diagram diagram)
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();
                var image = renderer.Render(diagram);
                var png = image.Encode();

                var pngPath = Path.Combine(tempDir, Path.ChangeExtension(Path.GetFileName(key.Path), "png"));
                using (var pngFile = File.OpenWrite(pngPath))
                {
                    png.SaveTo(pngFile);
                }
                logger.LogInformation("Generated {OutputFileFile} in {ElapsedMilliseconds}ms", pngPath, stopwatch.ElapsedMilliseconds);

                facade.SendNotification(new UpdatePreview { Uri = key, Filename = pngPath });
            }
            catch (Exception e)
            {
                logger.LogError(e, "Generation failed for {Uri}", key);
            }
        }
    }
}
