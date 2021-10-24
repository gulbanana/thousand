using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System.Threading;
using System.Threading.Tasks;
using Thousand.LSP.Analyse;
using Thousand.Parse.Attributes;

namespace Thousand.LSP.Handlers
{
    class HoverHandler : HoverHandlerBase
    {
        private readonly AnalysisService semanticService;
        private readonly IDiagnosticService diagnosticService;
        private readonly API metadata;

        public HoverHandler(AnalysisService semanticService, IDiagnosticService diagnosticService)
        {
            this.semanticService = semanticService;
            this.diagnosticService = diagnosticService;
            this.metadata = new API();
        }

        protected override HoverRegistrationOptions CreateRegistrationOptions(HoverCapability capability, ClientCapabilities clientCapabilities) => new HoverRegistrationOptions
        {
            DocumentSelector = DocumentSelector.ForLanguage("thousand")
        };

        public override async Task<Hover?> Handle(HoverParams request, CancellationToken cancellationToken)
        {
            if (diagnosticService.HasDiagnostics(request.TextDocument.Uri, request.Position))
            {
                return null;
            }

            var analysis = await semanticService.GetAnalysisAsync(request.TextDocument.Uri);

            foreach (var (uri, loc, ast) in analysis.ObjectReferences)
            {
                if (uri == request.TextDocument.Uri && loc.Contains(request.Position))
                {
                    var tooltip = Format.Canonicalise(ast);

                    return new Hover
                    {
                        Range = loc,
                        Contents = Format.CodeBlock(tooltip)
                    };
                }
            }

            foreach (var (uri, loc, ast) in analysis.ClassReferences)
            {
                if (uri == request.TextDocument.Uri && loc.Contains(request.Position) && ast is not null)
                {
                    var tooltip = Format.Canonicalise(ast);

                    return new Hover
                    {
                        Range = loc,
                        Contents = Format.CodeBlock(tooltip)
                    };
                }
            }

            if (analysis.Main != null)
            {
                foreach (var ctx in analysis.Main.Attributes)
                {
                    var key = ctx.Syntax.Key;
                    var loc = key.Span.AsRange();
                    if (loc.Contains(request.Position) && metadata.Documentation.ContainsKey(key.Text))
                    {
                        return new Hover
                        {
                            Range = key.Span.AsRange(),
                            Contents = new MarkedStringsOrMarkupContent(new MarkupContent { Kind = MarkupKind.Markdown, Value = metadata.Documentation[key.Text] })
                        };
                    }
                }
            }

            return null;
        }
    }
}
