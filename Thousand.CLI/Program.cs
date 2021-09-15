using System;
using System.Diagnostics;
using System.IO;

namespace Thousand.CLI
{
    class Program
    {
        private static readonly string graph = @"
    node ""Entity One""
    node ""Entity 😊""

    node ""‮‮Right-To-Left
Entity""

    node ""Three
Lined
Entity""

    node ""हिन्दी के साधन""
";

        static void Main()
        {
            var tokenizer = Tokenizer.Build();
            var document = Parsers.Document(tokenizer.Tokenize(graph));
            var diagram = Composer.Compose(document.Value);
            var image = Renderer.Render(diagram);

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
    }
}
