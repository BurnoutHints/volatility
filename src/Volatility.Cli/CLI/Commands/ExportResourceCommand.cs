using Volatility.Abstractions.Messaging;
using Volatility.Abstractions.Operations;
using Volatility.Abstractions.Services;
using Volatility.CLI;
using Volatility.Operations.Resources;
using Volatility.Resources;

namespace Volatility.CLI.Commands;

internal class ExportResourceCommand : ICommand
{
    private readonly IPathProvider pathProvider;
    private readonly IOperation<LoadResourceRequest, LoadResourceResult> loadOperation;
    private readonly IOperation<ExportResourceRequest, ExportResourceResult> exportOperation;

    public static string CommandToken => "ExportResource";
    public static string CommandDescription => "Exports information and relevant data from an imported/created resource into a platform's format.";
    public static string CommandParameters => "--recurse --overwrite --type=<resource type OR index> --format=<tub,bpr,x360,ps3> --respath=<data path> --outpath=<file/folder path> [--imports=<raw,bnd2manager,dgi,yap,volatility>] [--importsfile]";

    public string? Format { get; set; }
    public string? ResourcePath { get; set; }
    public string? OutputPath { get; set; }
    public string? Imports { get; set; }
    public bool ImportsFile { get; set; }
    public bool Overwrite { get; set; }
    public bool Recursive { get; set; }

    public async Task Execute()
    {
        if (string.IsNullOrEmpty(Format))
        {
            CLIMessageUtilities.Error<ExportResourceCommand>("Error: No resource path specified! (--respath)");
            return;
        }

        if (string.IsNullOrEmpty(ResourcePath))
        {
            CLIMessageUtilities.Error<ExportResourceCommand>("Error: No resource path specified! (--respath)");
            return;
        }

        if (string.IsNullOrEmpty(OutputPath))
        {
            CLIMessageUtilities.Error<ExportResourceCommand>("Error: No output path specified! (--outpath)");
            return;
        }

        string filePath = Path.Combine(pathProvider.GetDirectory(VolatilityPathLocation.Resources), ResourcePath);
        string[] sourceFiles = pathProvider.GetFilePaths(filePath, VolatilityFilePathFilter.Any, Recursive);

        if (sourceFiles.Length == 0)
        {
            CLIMessageUtilities.Error<ExportResourceCommand>($"Error: No valid file(s) found at the specified path ({ResourcePath}). Ensure the path exists and spaces are properly enclosed. (--path)");
            return;
        }

        if (!Volatility.Utilities.TypeUtilities.TryParseEnum(Format, out Platform platform))
        {
            CLIMessageUtilities.Error<ExportResourceCommand>("Error: Invalid file format specified!");
            return;
        }

        Unpacker? importUnpackerOverride = null;
        if (!string.IsNullOrEmpty(Imports) && !string.Equals(Imports, "DEFAULT", StringComparison.OrdinalIgnoreCase))
        {
            if (!Volatility.Utilities.TypeUtilities.TryParseEnum(Imports, out Unpacker parsedUnpacker))
            {
                CLIMessageUtilities.Error<ExportResourceCommand>("Error: Invalid imports export mode specified!");
                return;
            }

            importUnpackerOverride = parsedUnpacker;
        }

        bool multipleOutputs = sourceFiles.Length > 1;
        string inputRoot = pathProvider.DirectoryExists(filePath)
            ? pathProvider.GetFullPath(filePath)
            : Path.GetDirectoryName(pathProvider.GetFullPath(filePath)) ?? pathProvider.GetFullPath(filePath);

        List<Task> tasks = [];
        foreach (string sourceFile in sourceFiles)
        {
            CLIMessageUtilities.Info<ExportResourceCommand>(sourceFile, MessageCategory.Resource);

            tasks.Add(Task.Run(async () =>
            {
                if (!pathProvider.FileExists(sourceFile))
                {
                    CLIMessageUtilities.Error<ExportResourceCommand>("Error: Invalid file import path specified!");
                    return;
                }

                if (!Volatility.Utilities.TypeUtilities.TryParseEnum(Path.GetExtension(sourceFile).TrimStart('.'), out ResourceType resourceType))
                {
                    CLIMessageUtilities.Error<ExportResourceCommand>("Error: Resource type is invalid!");
                    return;
                }

                OperationResult<LoadResourceResult> loadResult = await loadOperation.ExecuteAsync(
                    new LoadResourceRequest(sourceFile, resourceType, platform),
                    progress: null,
                    cancellationToken: CancellationToken.None);
                CLIMessageUtilities.PublishIssues(loadResult.Issues, MessageCategory.Resource);

                if (!loadResult.Success || loadResult.Value == null)
                {
                    return;
                }

                string outputPath = ResolveOutputPath(sourceFile, inputRoot, OutputPath, multipleOutputs);

                OperationResult<ExportResourceResult> exportResult = await exportOperation.ExecuteAsync(
                    new ExportResourceRequest(
                        loadResult.Value.Resource,
                        outputPath,
                        platform,
                        importUnpackerOverride,
                        ImportsFile,
                        Overwrite),
                    progress: null,
                    cancellationToken: CancellationToken.None);
                CLIMessageUtilities.PublishIssues(exportResult.Issues, MessageCategory.Resource);

                if (!exportResult.Success)
                {
                    return;
                }

                CLIMessageUtilities.Success<ExportResourceCommand>(
                    $"Exported {Path.GetFileName(sourceFile)} as {pathProvider.GetFullPath(outputPath)}.",
                    MessageCategory.Resource);
            }));
        }

        await Task.WhenAll(tasks);
    }

    public void SetArgs(Dictionary<string, object> args)
    {
        Format = (args.TryGetValue("format", out object? format) ? format as string : "")?.ToUpper();
        ResourcePath = args.TryGetValue("respath", out object? respath) ? respath as string : "";
        OutputPath = args.TryGetValue("outpath", out object? outpath) ? outpath as string : "";
        Imports = args.TryGetValue("imports", out object? imports) ? imports as string : "";
        ImportsFile = args.TryGetValue("importsfile", out var importsfile) && (bool)importsfile;
        Overwrite = args.TryGetValue("overwrite", out var ow) && (bool)ow;
        Recursive = args.TryGetValue("recurse", out var re) && (bool)re;
    }

    public ExportResourceCommand(
        IPathProvider pathProvider,
        IOperation<LoadResourceRequest, LoadResourceResult> loadOperation,
        IOperation<ExportResourceRequest, ExportResourceResult> exportOperation)
    {
        this.pathProvider = pathProvider;
        this.loadOperation = loadOperation;
        this.exportOperation = exportOperation;
    }

    private static string ResolveOutputPath(
        string sourceFile,
        string inputRoot,
        string outputPath,
        bool multipleOutputs)
    {
        if (!multipleOutputs)
        {
            return outputPath;
        }

        string relativePath = Path.GetRelativePath(inputRoot, sourceFile);
        return Path.Combine(outputPath, relativePath);
    }
}
