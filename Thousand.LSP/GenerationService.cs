using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Xml;
using Thousand.Layout;
using Thousand.Render;

namespace Thousand.LSP
{
    class GenerationService : IGenerationService
    {
        private readonly string tempDir;
        private readonly HashSet<DocumentUri> current;
        private readonly SkiaRenderer pngRenderer;
        private readonly SVGRenderer svgRenderer;
        private readonly ILogger<GenerationService> logger;
        private readonly IOptionsMonitor<ServerOptions> options;
        private readonly ILanguageServerFacade facade;

        public GenerationService(ILogger<GenerationService> logger, IOptionsMonitor<ServerOptions> options, ILanguageServerFacade facade)
        {
            var tempPath = Path.GetTempPath();
            tempDir = Path.Combine(tempPath, "Thousand.LSP");
            Directory.CreateDirectory(tempDir);

            current = new HashSet<DocumentUri>();
            pngRenderer = new SkiaRenderer();
            svgRenderer = new SVGRenderer(true);

            this.logger = logger;
            this.options = options;
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

            _ = options.CurrentValue.GeneratePNG ? Task.Run(() => GenPNG(key, diagram)) : Task.Run(() => GenSVG(key, diagram));
        }

        private void GenPNG(DocumentUri key, Diagram diagram)
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();
                var image = pngRenderer.Render(diagram);
                var png = image.Encode();

                var pngPath = Path.Combine(tempDir, Path.ChangeExtension(Path.GetFileName(key.Path), "png"));
                using (var pngFile = File.OpenWrite(pngPath))
                {
                    png.SaveTo(pngFile);
                }
                logger.LogInformation("Generated {OutputFile} in {ElapsedMilliseconds}ms", pngPath, stopwatch.ElapsedMilliseconds);

                facade.SendNotification(new Extensions.UpdatePreview { Uri = key, Filename = pngPath });
            }
            catch (Exception e)
            {
                logger.LogError(e, "PNG generation failed for {Uri}", key);
            }
        }

        private void GenSVG(DocumentUri key, Diagram diagram)
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();
                var xml = svgRenderer.Render(diagram);

                var svgPath = Path.Combine(tempDir, Path.ChangeExtension(Path.GetFileName(key.Path), "svg"));
                using (var writer = XmlWriter.Create(svgPath))
                {
                    xml.WriteTo(writer);
                }

                logger.LogInformation("Generated {OutputFile} in {ElapsedMilliseconds}ms", svgPath, stopwatch.ElapsedMilliseconds);

                facade.SendNotification(new Extensions.UpdatePreview { Uri = key, Filename = svgPath });
            }
            catch (Exception e)
            {
                logger.LogError(e, "SVG generation failed for {Uri}", key);
            }
        }
    }
}
