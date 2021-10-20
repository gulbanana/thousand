using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System.Collections.Generic;
using System.Linq;
using Thousand.AST;
using Thousand.Layout;
using TextSpan = Superpower.Model.TextSpan;
using Token = Superpower.Model.Token<Thousand.Parse.TokenKind>;
using TokenList = Superpower.Model.TokenList<Thousand.Parse.TokenKind>;

namespace Thousand.LSP
{
    public class Analysis
    {
        public DocumentUri Uri { get; }
        // XXX add uint version, might help to avoid races

        public TokenList? Tokens { get; set; }
        public TolerantDocument? Syntax { get; set; }
        public TypedDocument? ValidSyntax { get; set; }
        public IR.Root? Rules { get; set; }
        public Diagram? Diagram { get; set; }

        public List<(TextSpan, TolerantClass)> ClassReferences { get; } = new();
        public List<(TextSpan, TolerantObject)> ObjectReferences { get; } = new();

        public Analysis(DocumentUri uri)
        {
            Uri = uri;
        }

        public Token? FindToken(Position position)
        {
            if (!Tokens.HasValue)
            {
                return default;
            }

            foreach (var token in Tokens)
            {
                var tokenRange = token.Span.AsRange();
                if (tokenRange.Contains(position))
                {
                    return token;
                }
            }

            return default;
        }
        

        // root only
        public Parse.Macro<TolerantDocumentContent>? FindDeclaration(Position position)
        {
            if (Syntax is null) return null;
            if (FindToken(position) is not Token token) return null;

            return Syntax.Declarations.Where(d => !d.Value.IsT0 && d.Sequence().Contains(token)).Select(d => d.Value.Match(
                _ => throw new System.NotSupportedException(),
                _ => d.Select(v => (TolerantDocumentContent)v.AsT1),
                _ => d.Select(v => (TolerantDocumentContent)v.AsT2),
                _ => d.Select(v => (TolerantDocumentContent)v.AsT3),
                _ => d.Select(v => (TolerantDocumentContent)v.AsT4)
            )).SingleOrDefault();
        }
    }
}
