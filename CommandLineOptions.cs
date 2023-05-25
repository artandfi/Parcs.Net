using CommandLine;
using Parcs.Module.CommandLine;

namespace JpegModule {
    public class CommandLineOptions : BaseModuleOptions {
        [Option("file", Required = true, HelpText = "Path to the bitmap to be encoded.")]
        public string BmpFileName { get; set; }

        [Option("p", Required = true, HelpText = "Number of points.")]
        public int PointsNum { get; set; }
    }
}
