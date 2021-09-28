﻿using OneOf;
using Superpower;
using System;
using System.Collections.Generic;
using System.IO;
using Thousand.Parse;

namespace Thousand
{
    /// <summary>Main high-level entry point</summary>
    public class DiagramGenerator<T> : IDisposable
    {
        public static string ReadStdlib()
        {
            using var resource = typeof(DiagramGenerator<>).Assembly.GetManifestResourceStream("Thousand.stdlib.1000");
            using var reader = new StreamReader(resource!);

            return reader.ReadToEnd();
        }

        private readonly Tokenizer<TokenKind> tokenizer;
        private readonly TokenListParser<TokenKind, AST.Document> parser;
        private readonly IRenderer<T> renderer;

        public DiagramGenerator(IRenderer<T> renderer)
        {
            this.renderer = renderer;
            tokenizer = Tokenizer.Build();
            parser = Parser.Build();            
        }

        public void Dispose()
        {
            renderer.Dispose();
        }

        private OneOf<GenerationResult<AST.Document>, GenerationError[]> Parse(string sourceFile)
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

            return new GenerationResult<AST.Document>(parsed.Value, Array.Empty<GenerationError>());
        }

        /// <summary>Create a diagram from source code.</summary>
        /// <param name="source">.1000 source text.</param>
        /// <returns>Either the generated diagram (as a data structure) and zero-or-more-warnings, or one-or-more errors.</returns>
        public OneOf<GenerationResult<Layout.Diagram>, GenerationError[]> GenerateDiagram(string source, bool stdlib = true)
        {
            var sources = new List<string>();
            if (stdlib) sources.Add(ReadStdlib());
            sources.Add(source);

            var documents = new List<AST.Document>();
            foreach (var s in sources)
            {
                var result = Parse(s);
                if (result.IsT1)
                {
                    return result.AsT1;
                }
                else
                {
                    documents.Add(result.AsT0.Diagram);
                }
            }

            var warnings = new List<GenerationError>();
            var errors = new List<GenerationError>();

            if (!Evaluator.TryEvaluate(documents, warnings, errors, out var rules))
            {
                return errors.ToArray();
            }

            var measuredText = renderer.MeasureTextBlocks(rules);

            if (!Composer.TryCompose(rules, measuredText, warnings, errors, out var diagram))
            {
                return errors.ToArray();
            }

            return new GenerationResult<Layout.Diagram>(diagram, warnings.ToArray());
        }

        /// <summary>Create an image from source code.</summary>
        /// <param name="source">.1000 source text.</param>
        /// <returns>Either the generated diagram (as a Skia image) and zero-or-more-warnings, or one-or-more errors.</returns>
        public OneOf<GenerationResult<T>, GenerationError[]> GenerateImage(string source, bool stdlib = true)
        {
            return GenerateDiagram(source, stdlib).MapT0(result =>
            {
                var image = renderer.Render(result.Diagram);
                return new GenerationResult<T>(image, result.Warnings);
            });
        }

        /// <summary>Create an image from a diagram.</summary>
        /// <param name="diagram">Data structure produced from a .1000 file.</param>
        /// <returns>Either the generated diagram (as a Skia image) and zero-or-more-warnings, or one-or-more errors.</returns>
        public T GenerateImage(Layout.Diagram diagram)
        {
            return renderer.Render(diagram);
        }
    }
}
