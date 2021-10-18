using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using System.Collections.Generic;
using System.Linq;

namespace Thousand.LSP
{
    public class DiagnosticService : IDiagnosticService
    {
        private readonly Dictionary<DocumentUri, List<Diagnostic>> current = new();
        private readonly ILanguageServerFacade facade;
        private bool? supported;

        public DiagnosticService(ILanguageServerFacade facade)
        {
            this.facade = facade;            
        }

        public bool HasDiagnostics(DocumentUri uri, Position position)
        {
            return current.ContainsKey(uri) && current[uri].Any(d => d.Range.Contains(position));
        }

        public void Track(DocumentUri uri)
        {
            current.Add(uri, new List<Diagnostic>());
        }

        public void Untrack(DocumentUri uri)
        {
            current.Remove(uri);
        }

        public void Update(DocumentUri uri, GenerationState state)
        {
            if (!current.ContainsKey(uri))
            { 
                return; 
            }

            if (!supported.HasValue)
            {
                supported = facade.ClientSettings.Capabilities?.TextDocument?.PublishDiagnostics.IsSupported ?? false;
            }

            if (!supported.Value)
            {
                return;
            }

            var warnings = state.GetWarnings().Select(w => new Diagnostic
            {
                Severity = DiagnosticSeverity.Warning,
                Source = "thousand",
                Range = w.Span.AsRange(),
                Code = w.Kind.ToString(),
                Message = w.Message
            });

            var errors = state.GetErrors().Select(w => new Diagnostic
            {
                Severity = DiagnosticSeverity.Error,
                Source = "thousand",
                Range = w.Span.AsRange(),
                Code = w.Kind.ToString(),
                Message = w.Message
            });

            var all = warnings.Concat(errors).Distinct().ToList();
            current[uri] = all;

            facade.TextDocument.PublishDiagnostics(new PublishDiagnosticsParams
            {
                Uri = uri,
                Diagnostics = all
            });
        }
    }
}
