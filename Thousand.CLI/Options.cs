using CommandLine;

namespace Thousand.CLI
{
    public class Options
    {
        [Value(index: 0, MetaName = "input", Required = true, HelpText = "Input .1000 file.")]
        public string Input { get; set; } = default!;

        [Value(index: 1, MetaName = "output", Required = false, HelpText = "Output image file.")]
        public string? Output { get; set; }

        [Option(shortName: 't', longName: "output-type", Required = false, HelpText = "(png|webp|svg)", Default = OutputType.png)]
        public OutputType Type { get; set; }

        [Option(shortName: 'o', longName: "open-file", Required = false, HelpText = "Open the image file after diagram generation.", Default = false)]
        public bool OpenFile { get; set; }
    }
}