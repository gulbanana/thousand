using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System.Collections.Generic;
using Thousand.AST;
using Thousand.Layout;
using TokenList = Superpower.Model.TokenList<Thousand.Parse.TokenKind>;

namespace Thousand.LSP.Analyse
{
    public class Analysis
    {
        // extend to arbitrary refs?
        public ParsedDocument? Stdlib { get; set; }
        public ParsedDocument? Main { get; set; }

        public TokenList? Tokens { get; set; }        
        public TypedDocument? ValidSyntax { get; set; }
        public IR.Root? Rules { get; set; }
        public Diagram? Diagram { get; set; }

        public Dictionary<UntypedClass, Location> ClassDefinitions { get; } = new();
        public Dictionary<UntypedObject, Location> ObjectDefinitions { get; } = new();
        
        public List<Located<UntypedClass?>> ClassReferences { get; } = new();        
        public List<Located<UntypedObject>> ObjectReferences { get; } = new();

        // XXX only used for defunct go-to-type-definition
        public Dictionary<UntypedClass, List<UntypedClass>> ClassClasses { get; } = new();
        public Dictionary<UntypedObject, List<UntypedClass>> ObjectClasses { get; } = new();
    }
}
