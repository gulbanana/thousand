using OneOf;
using SkiaSharp;
using Superpower;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Thousand.Parse;

namespace Thousand
{
    /// <summary>Main high-level entry point</summary>
    public class DiagramGenerator : IDisposable
    {
        public static string ReadStdlib()
        {
            using var resource = typeof(DiagramGenerator).Assembly.GetManifestResourceStream("Thousand.stdlib.1000");
            using var reader = new StreamReader(resource!);

            return reader.ReadToEnd();
        }

        private readonly Tokenizer<TokenKind> tokenizer;
        private readonly TokenListParser<TokenKind, AST.Diagram> parser;
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

        private OneOf<GenerationResult<AST.Diagram>, GenerationError[]> Parse(string sourceFile)
        {
            var tokenized = tokenizer.TryTokenize(sourceFile);
            if (!tokenized.HasValue)
            {
                return new GenerationError[] { new(tokenized.ToString()) };
            }

            var parsed = parser(tokenized.Value);
            if (!parsed.HasValue)
            {
                return new GenerationError[] { new(parsed.ToString()) };
            }

            return new GenerationResult<AST.Diagram>(parsed.Value, Array.Empty<GenerationError>());
        }

        /// <summary>Create a diagram from source code.</summary>
        /// <param name="source">.1000 source text.</param>
        /// <returns>Either the generated diagram (as a data structure) and zero-or-more-warnings, or one-or-more errors.</returns>
        public OneOf<GenerationResult<Layout.Diagram>, GenerationError[]> GenerateDiagram(string source, bool stdlib = true)
        {
            var sources = new List<string>();
            if (stdlib) sources.Add(ReadStdlib());
            sources.Add(source);

            var parses = new List<AST.Diagram>();
            foreach (var s in sources)
            {
                var result = Parse(s);
                if (result.IsT1)
                {
                    return result.AsT1;
                }
                else
                {
                    parses.Add(result.AsT0.Diagram);
                }
            }

            if (!Composer.TryCompose(parses, out var diagram, out var warnings, out var errors))
            {
                return errors;
            }

            return new GenerationResult<Layout.Diagram>(diagram, warnings);
        }

        /// <summary>Create a diagram from source code.</summary>
        /// <param name="source">.1000 source text.</param>
        /// <returns>Either the generated diagram (as a Skia image) and zero-or-more-warnings, or one-or-more errors.</returns>
        public OneOf<GenerationResult<SKImage>, GenerationError[]> GenerateImage(string source, bool stdlib = true)
        {
            return GenerateDiagram(source, stdlib).MapT0(result =>
            {
                var image = renderer.Render(result.Diagram);
                return new GenerationResult<SKImage>(image, result.Warnings);
            });
        }
    }
}
