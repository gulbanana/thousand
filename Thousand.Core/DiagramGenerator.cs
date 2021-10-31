using OneOf;
using System;
using System.Collections.Generic;
using System.IO;

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

        private readonly API.Metadata api;
        private readonly Superpower.Tokenizer<Parse.TokenKind> tokenizer;

        public DiagramGenerator()
        {
            api = new();
            tokenizer = new Parse.Tokenizer();
        }

        /// <summary>Create a diagram from source code.</summary>
        /// <param name="source">.1000 source text.</param>
        /// <returns>Either the generated diagram (as a data structure) and zero-or-more-warnings, or one-or-more errors.</returns>
        public OneOf<GenerationResult<Layout.Diagram>, GenerationError[]> GenerateDiagram(string source, bool stdlib = true)
        {
            var sources = new List<string>();
            if (stdlib) sources.Add(ReadStdlib());
            sources.Add(source);

            var state = new GenerationState();

            var documents = new List<AST.TypedDocument>();
            foreach (var s in sources)
            {
                var end = Parse.Shared.GetEnd(s);
                var tokens = tokenizer.TryTokenize(s);
                if (tokens.HasValue)
                {
                    if (Parse.Preprocessor.TryPreprocess(state, end, tokens.Value, out var syntax) && Parse.Typechecker.TryTypecheck(api, state, syntax, end, allowErrors: false, out var document))
                    {
                        documents.Add(document);
                    }
                }
                else
                {
                    state.AddError(tokens.Location, ErrorKind.Syntax, tokens.FormatErrorMessageFragment());
                }
            }

            if (state.HasErrors() || !Evaluate.Evaluator.TryEvaluate(documents, state, out var root))
            {
                return state.GetErrors();
            }

            if (!Compose.Composer.TryCompose(root, state, out var diagram))
            {
                return state.GetErrors();
            }

            return new GenerationResult<Layout.Diagram>(diagram, state.GetWarnings());
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
                return new GenerationError[] { new(Superpower.Model.TextSpan.Empty, ex) };
            }
        }
    }
}
