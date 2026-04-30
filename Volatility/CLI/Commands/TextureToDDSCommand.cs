using Volatility.Abstractions.Messaging;
using Volatility.Abstractions.Services;
using Volatility.Operations.Resources;
using Volatility.Resources;
using Volatility.Utilities;

namespace Volatility.CLI.Commands;

internal class TextureToDDSCommand : ICommand
{
    private readonly IPathProvider pathProvider;
    private readonly TextureToDDSOperation operation;

    public static string CommandToken => "TextureToDDS";
    public static string CommandDescription => "Converts texture resources and their sidecar bitmap data into DDS files.";
    public static string CommandParameters => "[--recurse] [--overwrite] [--verbose] --format=<tub,bpr[x64],x360,ps3> --path=<file/folder path> [--outpath=<file/folder path>]";

    public string? Format { get; set; }
    public string? InputPath { get; set; }
    public string? OutputPath { get; set; }
    public bool Overwrite { get; set; }
    public bool Recursive { get; set; }
    public bool Verbose { get; set; }

    public async Task Execute()
    {
        if (string.IsNullOrWhiteSpace(Format))
        {
            CLIMessageUtilities.Error<TextureToDDSCommand>("Error: No input format specified! (--format)");
            return;
        }

        if (string.IsNullOrWhiteSpace(InputPath))
        {
            CLIMessageUtilities.Error<TextureToDDSCommand>("Error: No input path specified! (--path)");
            return;
        }

        string[] sourceFiles = pathProvider.GetFilePaths(InputPath, VolatilityFilePathFilter.Header, Recursive);
        if (sourceFiles.Length == 0)
        {
            CLIMessageUtilities.Error<TextureToDDSCommand>($"Error: No valid file(s) found at the specified path ({InputPath}). Ensure the path exists and spaces are properly enclosed. (--path)");
            return;
        }

        string formatValue = Format;
        bool isX64 = formatValue.EndsWith("X64", StringComparison.OrdinalIgnoreCase);
        if (isX64)
        {
            formatValue = formatValue[..^3];
        }

        if (!TypeUtilities.TryParseEnum(formatValue, out Platform platform))
        {
            throw new InvalidPlatformException("Error: Invalid file format specified!");
        }

        CLIMessageUtilities.Info<TextureToDDSCommand>(
            $"Starting {sourceFiles.Length} Texture to DDS tasks...",
            MessageCategory.Texture);

        await operation.ExecuteAsync(sourceFiles, platform, isX64, OutputPath, Overwrite, Verbose);
    }

    public void SetArgs(Dictionary<string, object> args)
    {
        Format = (args.TryGetValue("format", out object? format) ? format as string : "")?.ToUpper();
        InputPath = args.TryGetValue("path", out object? path) ? path as string : "";
        OutputPath = args.TryGetValue("outpath", out object? outpath) ? outpath as string : "";
        Overwrite = args.TryGetValue("overwrite", out var ow) && (bool)ow;
        Recursive = args.TryGetValue("recurse", out var re) && (bool)re;
        Verbose = args.TryGetValue("verbose", out var ve) && (bool)ve;
    }

    public TextureToDDSCommand(IPathProvider pathProvider, TextureToDDSOperation operation)
    {
        this.pathProvider = pathProvider;
        this.operation = operation;
    }
}
