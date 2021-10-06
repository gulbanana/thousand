using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Thousand.CLI
{
    class Program
    {
        static void Main(string[] args)
        {
            var graph = File.ReadAllText(args.Single());

            using var generator = new Render.SkiaDiagramGenerator();

            generator.GenerateImage(graph).Switch(result =>
            {
                var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "thousand-test.png");

                using (var output = File.OpenWrite(path))
                {
                    result.Diagram.Encode().SaveTo(output);
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = path,
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
