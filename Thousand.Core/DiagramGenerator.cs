using OneOf;
using System;
using System.Collections.Generic;
using System.IO;
using Thousand.Parse;

namespace Thousand
{
    /// <summary>Main high-level entry point</summary>
    public class DiagramGenerator
    {
        public static string ReadStdlib()
        {
            using var resource = typeof(DiagramGenerator<>).Assembly.GetManifestResourceStream("Thousand.stdlib.1000");
            using var reader = new StreamReader(resource!);

            return reader.ReadToEnd();
        }

        private OneOf<GenerationResult<AST.TypedDocument>, GenerationError[]> Parse(string sourceFile)
        {
            var warnings = new List<GenerationError>();
            var errors = new List<GenerationError>();

            if (!Parser.TryParse(sourceFile, warnings, errors, out var document))
            {
                return errors.ToArray();
            }
            else
            {
                return new GenerationResult<AST.TypedDocument>(document, warnings.ToArray());
            }
        }

        /// <summary>Create a diagram from source code.</summary>
        /// <param name="source">.1000 source text.</param>
        /// <returns>Either the generated diagram (as a data structure) and zero-or-more-warnings, or one-or-more errors.</returns>
        public OneOf<GenerationResult<Layout.Diagram>, GenerationError[]> GenerateDiagram(string source, bool stdlib = true)
        {
            var sources = new List<string>();
            if (stdlib) sources.Add(ReadStdlib());
            sources.Add(source);

            var documents = new List<AST.TypedDocument>();
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

            if (!Compose.Composer.TryCompose(rules, warnings, errors, out var diagram))
            {
                return errors.ToArray();
            }

            return new GenerationResult<Layout.Diagram>(diagram, warnings.ToArray());
        }
    }

    /// <summary>Main high-level entry point, parameterised by a concrete renderer</summary>
    public class DiagramGenerator<T> : DiagramGenerator, IDisposable
    {
        private readonly Render.IRenderer<T> renderer;

        public DiagramGenerator(Render.IRenderer<T> renderer)
        {
            this.renderer = renderer;
        }

        public void Dispose()
        {
            renderer.Dispose();
        }

        /// <summary>Create an image from source code.</summary>
        /// <param name="source">.1000 source text.</param>
        /// <returns>Either the generated diagram (as a Skia image) and zero-or-more-warnings, or one-or-more errors.</returns>
        public OneOf<GenerationResult<T>, GenerationError[]> GenerateImage(string source, bool stdlib = true)
        {
            var diagram = GenerateDiagram(source, stdlib);
            if (diagram.IsT1)
            {
                return diagram.AsT1;
            }
            else
            {
                var image = GenerateImage(diagram.AsT0.Diagram);
                if (image.IsT0)
                {
                    return new GenerationResult<T>(image.AsT0, diagram.AsT0.Warnings);
                }
                else
                {
                    return image.AsT1;
                }
            }
        }

        /// <summary>Create an image from a diagram.</summary>
        /// <param name="diagram">Data structure produced from a .1000 file.</param>
        /// <returns>Either the generated diagram (as a Skia image), or a thrown exception.</returns>
        public OneOf<T, GenerationError[]> GenerateImage(Layout.Diagram diagram)
        {
            try
            {
                return renderer.Render(diagram);
            }
            catch (Exception ex)
            {
                return new GenerationError[] { new(ex) };
            }
        }
    }
}
