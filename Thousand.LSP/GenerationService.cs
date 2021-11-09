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
using Thousand.LSP.Analyse;
using Thousand.Render;

namespace Thousand.LSP
{
    class GenerationService : IGenerationService
    {
        private readonly string tempDir;
        private readonly HashSet<DocumentUri> current;
        private readonly SkiaRenderer pngRenderer;
        private readonly SVGRenderer previewSvgRenderer;
        private readonly SVGRenderer finalSvgRenderer;
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
            previewSvgRenderer = new SVGRenderer(true);
            finalSvgRenderer = new SVGRenderer(false);

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

            _ = Task.Run(() =>
            {
                try
                {
                    var path = options.CurrentValue.GeneratePNG ? GenPNG(key, diagram) : GenSVG(key, diagram, true);
                    facade.SendNotification(new Extensions.UpdatePreview { Uri = key, Filename = path });
                }
                catch (Exception e)
                {
                    logger.LogError(e, "SVG generation failed for {Uri}", key);
                }
            });
        }

        public string? Export(DocumentUri key, Analysis analysis, bool png)
        {
            if (analysis.Diagram == null)
            {
                return null;
            }

            if (png)
            {
                return GenPNG(key, analysis.Diagram);
            }

            // for final SVG output, re-run generation from the intermediate representation forward, *without* the grouping and class names
            else
            {
                var state = new GenerationState();
                if (Compose.Composer.TryCompose(analysis.Root!, state, false, out var diagram))
                {
                    return GenSVG(key, diagram, false);
                }
                else
                {
                    return null;
                }
            }
        }

        private string GenPNG(DocumentUri key, Diagram diagram)
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

            return pngPath;
        }

        private string GenSVG(DocumentUri key, Diagram diagram, bool includeMetadata)
        {
            var stopwatch = Stopwatch.StartNew();
            var xml = (includeMetadata ? previewSvgRenderer : finalSvgRenderer).Render(diagram);

            var svgPath = Path.Combine(tempDir, Path.ChangeExtension(Path.GetFileName(key.Path), "svg"));
            using (var writer = XmlWriter.Create(svgPath))
            {
                xml.WriteTo(writer);
            }

            logger.LogInformation("Generated {OutputFile} in {ElapsedMilliseconds}ms", svgPath, stopwatch.ElapsedMilliseconds);

            return svgPath;
        }
    }
}
