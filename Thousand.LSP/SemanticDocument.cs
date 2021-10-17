using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System;
using System.Linq;
using Thousand.AST;
using Thousand.Layout;
using Token = Superpower.Model.Token<Thousand.Parse.TokenKind>;
using TokenList = Superpower.Model.TokenList<Thousand.Parse.TokenKind>;

namespace Thousand.LSP
{
    public class SemanticDocument
    {
        public DocumentUri Uri { get; }
        // XXX add uint version, might help to avoid races

        public TokenList? Tokens { get; set; }
        public TolerantDocument? Syntax { get; set; }
        public Diagram? Diagram { get; set; }

        public SemanticDocument(DocumentUri uri)
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
                _ => throw new Exception(),
                _ => d.Select(v => (TolerantDocumentContent)v.AsT1),
                _ => d.Select(v => (TolerantDocumentContent)v.AsT2),
                _ => d.Select(v => (TolerantDocumentContent)v.AsT3),
                _ => d.Select(v => (TolerantDocumentContent)v.AsT4)
            )).SingleOrDefault();
        }
    }
}
