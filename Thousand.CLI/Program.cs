using System;
using System.Diagnostics;
using System.IO;

namespace Thousand.CLI
{
    class Program
    {
        static void Main()
        {
            var graph = File.ReadAllText(@"demo.1000");

            using var generator = new DiagramGenerator();

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
                    Console.WriteLine(error);
                }

                Console.ReadKey();
            });
        }
    }
}
