using BenchmarkDotNet.Attributes;
using Superpower;
using Superpower.Model;
using System.IO;
using Thousand.Parse;

namespace Thousand.Benchmarks
{
    public class Parsers
    {
        [Params("connectors.1000", "tetris.1000", "underground.1000")]
        public string Input { get; set; } = default!;    
        
        private readonly Tokenizer<TokenKind> tokenizer;
        private string source;
        private TokenList<TokenKind> tokens;

        public Parsers()
        {
            tokenizer = Parse.Tokenizer.Build();
        }

        [GlobalSetup]
        public void Setup()
        {
            source = File.ReadAllText(Input);
            tokens = tokenizer.Tokenize(source);
        }

        [Benchmark]
        public TokenListParserResult<TokenKind, AST.UntypedDocument> Untyped()
        {
            return Parse.Untyped.Document(tokens);
        }

        [Benchmark]
        public TokenListParserResult<TokenKind, AST.TolerantDocument> Tolerant()
        {
            return Parse.Tolerant.Document(tokens);
        }
    }
}
