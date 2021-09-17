using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Thousand.CLI
{
    class Program
    {
        static void Main()
        {
            var graph = File.ReadAllText("test.1000");

            if (!TryParse(graph, out var document, out var error))
            {
                Console.WriteLine(error);
                Console.ReadKey();
                return;
            }

            var diagram = Composer.Compose(document);
            using var renderer = new Renderer();
            var image = renderer.Render(diagram);

            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "thousand-test.png");

            using (var output = File.OpenWrite(path))
            {
                image.Encode().SaveTo(output);
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            });
        }

        static bool TryParse(string source, [NotNullWhen(true)] out AST.Document? doc, [NotNullWhen(false)] out string? error)
        {
            var tokenizer = Tokenizer.Build();
            var parser = Parser.Build();

            doc = null;
            error = default(string?);

            var tokenized = tokenizer.TryTokenize(source);
            if (!tokenized.HasValue)
            {
                error = tokenized.ToString();
            }
            else
            {
                var parsed = parser(tokenized.Value);
                if (!parsed.HasValue)
                {
                    error = parsed.ToString();
                }
                else
                {
                    doc = parsed.Value;
                    return true;
                }
            }

            return false;
        }
    }
}
