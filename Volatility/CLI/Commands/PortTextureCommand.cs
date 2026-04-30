using Volatility.Abstractions.Messaging;
using Volatility.Abstractions.Services;
using Volatility.Operations.Resources;

namespace Volatility.CLI.Commands;

internal class PortTextureCommand : ICommand
{
    private readonly IPathProvider pathProvider;
    private readonly PortTextureOperation operation;

    public static string CommandToken => "PortTexture";
    public static string CommandDescription => "Ports texture data from a given source format to the specified destination format.";
    public static string CommandParameters => "[--verbose] [--usegtf] --informat=<tub,bpr[x64],x360,ps3> --inpath=<file/folder path> --outformat=<tub,bpr[x64],x360,ps3> [--outpath=<file/folder path>]";

    public string? SourceFormat { get; set; }
    public string? SourcePath { get; set; }
    public string? DestinationFormat { get; set; }
    public string? DestinationPath { get; set; }
    public bool Verbose { get; set; }
    public bool UseGTF { get; set; }

    public async Task Execute()
    {
        if (string.IsNullOrEmpty(SourcePath))
        {
            CLIMessageUtilities.Error<PortTextureCommand>("Error: No source path specified! (--inpath)");
            return;
        }

        string[] sourceFiles = pathProvider.GetFilePaths(SourcePath, VolatilityFilePathFilter.Header);
        if (sourceFiles.Length == 0)
        {
            CLIMessageUtilities.Error<PortTextureCommand>($"Error: No valid file(s) found at the specified path ({SourcePath}). Ensure the path exists and spaces are properly enclosed. (--inpath)");
            return;
        }

        CLIMessageUtilities.Info<PortTextureCommand>(
            $"Starting {sourceFiles.Length} PortTexture tasks...",
            MessageCategory.Texture);

        await operation.ExecuteAsync(sourceFiles, SourceFormat ?? string.Empty, SourcePath ?? string.Empty, DestinationFormat ?? string.Empty, DestinationPath, Verbose, UseGTF);
    }

    public void SetArgs(Dictionary<string, object> args)
    {
        SourceFormat = (args.TryGetValue("informat", out object? informat) ? informat as string
                : args.TryGetValue("if", out object? iff) ? iff as string : "auto").ToUpper();

        SourcePath = args.TryGetValue("inpath", out object? inpath) ? inpath as string
                : args.TryGetValue("ip", out object? ipp) ? ipp as string : "";

        DestinationFormat = (args.TryGetValue("outformat", out object? outformat) ? outformat as string
                : args.TryGetValue("of", out object? off) ? off as string : "auto").ToUpper();

        DestinationPath = args.TryGetValue("outpath", out object? outpath) ? outpath as string
                : args.TryGetValue("op", out object? opp) ? opp as string : SourcePath;

        Verbose = args.TryGetValue("verbose", out var verbose) && (bool)verbose;
        UseGTF = args.TryGetValue("usegtf", out var usegtf) && (bool)usegtf;
    }

    public PortTextureCommand(IPathProvider pathProvider, PortTextureOperation operation)
    {
        this.pathProvider = pathProvider;
        this.operation = operation;
    }
}
