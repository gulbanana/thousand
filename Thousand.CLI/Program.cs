using System;
using System.Diagnostics;
using System.IO;

namespace Thousand.CLI
{
    class Program
    {
        static void Main(string[] args)
        {
            var input = File.ReadAllText(args[0]);
            var output = args.Length > 1 ? args[1] : null;

            using var generator = new Render.SkiaDiagramGenerator();

            generator.GenerateImage(input).Switch(result =>
            {
                var outputPath = output ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "thousand-test.png");

                using (var outputFile = File.OpenWrite(outputPath))
                {
                    result.Diagram.Encode().SaveTo(outputFile);
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = outputPath,
                    UseShellExecute = true
                });
            }, errors =>
            {
                foreach (var error in errors)
                {
                    Console.WriteLine(error.ToString());
                }

                Console.ReadKey();
            });
        }
    }
}
