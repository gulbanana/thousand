using CommandLine;
using SkiaSharp;
using System;
using System.Diagnostics;
using System.IO;
using System.Xml.Linq;

namespace Thousand.CLI
{
    class Program
    {
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args).WithParsed(Generate);
        }

        static void Generate(Options options)
        {
            if (!File.Exists(options.Input))
            {
                Console.WriteLine($"Argument error: file `{options.Input}` not found.");
                return;
            }

            var outputPath = options.Output ?? Path.ChangeExtension(options.Input, options.Type switch
            {
                OutputType.svg => "svg",
                OutputType.png => "png",
                OutputType.webp => "webp"
            });

            if (!Directory.Exists(Path.GetDirectoryName(Path.GetFullPath(outputPath))))
            {
                Console.WriteLine($"Argument error: directory `{Path.GetDirectoryName(outputPath)}` not found.");
                return;
            }

            var source = File.ReadAllText(options.Input);

            if (options.Type == OutputType.svg)
            {
                using var generator = new DiagramGenerator<XElement>(new Render.SVGRenderer());
                generator.GenerateImage(source).Switch(result =>
                {
                    foreach (var warning in result.Warnings)
                    {
                        Console.WriteLine(warning.ToString());
                    }

                    new XDocument(result.Diagram).Save(outputPath);
                }, errors =>
                {
                    foreach (var error in errors)
                    {
                        Console.WriteLine(error.ToString());
                        return;
                    }
                });
            }
            else
            {
                using var generator = new DiagramGenerator<SKImage>(new Render.SkiaRenderer());
                generator.GenerateImage(source).Switch(result =>
                {
                    foreach (var warning in result.Warnings)
                    {
                        Console.WriteLine(warning.ToString());
                    }

                    using (var outputFile = File.OpenWrite(outputPath))
                    {
                        switch (options.Type)
                        {
                            case OutputType.png:
                                result.Diagram.Encode(SKEncodedImageFormat.Png, 100).SaveTo(outputFile);
                                break;

                            case OutputType.webp:
                                result.Diagram.PeekPixels().Encode(new SKWebpEncoderOptions(SKWebpEncoderCompression.Lossless, 100)).SaveTo(outputFile);
                                break;
                        }
                    }
                }, errors =>
                {
                    foreach (var error in errors)
                    {
                        Console.WriteLine(error.ToString());
                        return;
                    }
                });
            }

            Console.WriteLine("Created " + Path.GetFileName(outputPath));

            if (options.OpenFile)
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = Path.GetFullPath(outputPath),
                    UseShellExecute = true
                });
            }
        }
    }
}
