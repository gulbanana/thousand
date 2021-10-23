using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Server;
using System;
using System.Linq;
using System.Threading.Tasks;
using Thousand.LSP.Handlers;

namespace Thousand.LSP
{
    class Program
    {
        static async Task Main(string[] args)
        {
            if (args.Any(a => a.Equals("launchDebugger", StringComparison.OrdinalIgnoreCase)))
            {
                System.Diagnostics.Debugger.Launch();
            }

            var server = await LanguageServer.From(Configure);

            await server.WaitForExit;
        }

        private static void Configure(LanguageServerOptions options)
        {
            options
                .WithInput(Console.OpenStandardInput())
                .WithOutput(Console.OpenStandardOutput())
                .ConfigureLogging(builder => builder.AddLanguageProtocolLogging().SetMinimumLevel(LogLevel.Debug))
                .WithHandler<TextDocumentSyncHandler>()
                .WithHandler<HoverHandler>()
                .WithHandler<SemanticTokensHandler>()
                .WithHandler<ReferencesHandler>()
                .WithHandler<TypeDefinitionHandler>() // of questionable utility
                .WithHandler<VFSHandler>()
                .WithServices(ConfigureServices);
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(new ConfigurationItem { Section = "thousand" });
            services.AddSingleton<BufferService>();
            services.AddSingleton<Analyse.AnalysisService>();
            services.AddSingleton<IDiagnosticService, DiagnosticService>();
            services.AddSingleton<IGenerationService, GenerationService>();
        }
    }
}
