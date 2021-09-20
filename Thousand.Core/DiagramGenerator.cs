using OneOf;
using SkiaSharp;
using Superpower;
using System;
using System.Linq;
using Thousand.Parse;

namespace Thousand
{
    /// <summary>Main high-level entry point</summary>
    public class DiagramGenerator : IDisposable
    {
        private readonly Tokenizer<TokenKind> tokenizer;
        private readonly TokenListParser<TokenKind, AST.Document> parser;
        private readonly Render.Renderer renderer;

        public DiagramGenerator()
        {
            tokenizer = Tokenizer.Build();
            parser = Parser.Build();
            renderer = new Render.Renderer();
        }

        public void Dispose()
        {
            renderer.Dispose();
        }

        /// <summary>Create a diagram from source code.</summary>
        /// <param name="source">.1000 source text.</param>
        /// <returns>Either the generated diagram (as a data structure) and zero-or-more-warnings, or one-or-more errors.</returns>
        public OneOf<GenerationResult<Layout.Diagram>, GenerationError[]> GenerateDiagram(string source)
        {           
            var tokenized = tokenizer.TryTokenize(source);
            if (!tokenized.HasValue)
            {
                return new GenerationError[] { new(tokenized.ToString()) };
            }

            var parsed = parser(tokenized.Value);
            if (!parsed.HasValue)
            {
                return new GenerationError[] { new(parsed.ToString()) };
            }            

            if (!Composer.TryCompose(parsed.Value, out var diagram, out var warnings, out var errors))
            {
                return errors;
            }

            return new GenerationResult<Layout.Diagram>(diagram, warnings);
        }

        /// <summary>Create a diagram from source code.</summary>
        /// <param name="source">.1000 source text.</param>
        /// <returns>Either the generated diagram (as a Skia image) and zero-or-more-warnings, or one-or-more errors.</returns>
        public OneOf<GenerationResult<SKImage>, GenerationError[]> GenerateImage(string source)
        {
            return GenerateDiagram(source).MapT0(result =>
            {
                var image = renderer.Render(result.Diagram);
                return new GenerationResult<SKImage>(image, result.Warnings);
            });
        }
    }
}
