using Volatility.Abstractions.Messaging;
using Volatility.Abstractions.Operations;
using Volatility.Abstractions.Services;
using Volatility.Operations;
using Volatility.Operations.Resources;
using Volatility.Resources;
using Volatility.Utilities;

namespace Volatility.CLI.Commands;

internal class ImportResourceCommand : ICommand
{
        private readonly IPathProvider pathProvider;
        private readonly ImportResourceOperation importOperation;
        private readonly SaveResourceOperation saveOperation;

        public static string CommandToken => "ImportResource";
        public static string CommandDescription => "Imports information and relevant data from a specified platform's resource into a standardized format.";
        public static string CommandParameters => "--recurse --overwrite --type=<resource type OR index> --format=<tub,bpr,x360,ps3> --path=<file path>";

        public string? ResType { get; set; }
        public string? Format { get; set; }
        public string? ImportPath { get; set; }
        public bool Overwrite { get; set; }
        public bool Recursive { get; set; }

        public async Task Execute()
        {
                if (ResType == "AUTO")
                {
                        CLIMessageUtilities.Error<ImportResourceCommand>("Error: Automatic typing is not supported yet! Please specify a type (--type)");
                        return;
                }

                if (string.IsNullOrEmpty(ImportPath))
                {
                        CLIMessageUtilities.Error<ImportResourceCommand>("Error: No import path specified! (--path)");
                        return;
                }

                string fullImportPath = pathProvider.GetFullPath(ImportPath);
                if (!pathProvider.FileExists(fullImportPath) && !pathProvider.DirectoryExists(fullImportPath))
                {
                        CLIMessageUtilities.Error<ImportResourceCommand>("Error: Invalid file import path specified!");
                        return;
                }

                string[] sourceFiles = pathProvider.GetFilePaths(fullImportPath, VolatilityFilePathFilter.Header, Recursive);

                if (sourceFiles.Length == 0)
                {
                        CLIMessageUtilities.Error<ImportResourceCommand>($"Error: No valid file(s) found at the specified path ({ImportPath}). Ensure the path exists and spaces are properly enclosed. (--path)");
                        return;
                }

                string formatValue = Format ?? string.Empty;
                bool isX64 = formatValue.EndsWith("x64", StringComparison.OrdinalIgnoreCase);
                if (isX64)
                        formatValue = formatValue[..^3];

                if (!TypeUtilities.TryParseEnum(formatValue, out Platform platform))
                {
                        throw new InvalidPlatformException("Error: Invalid file format specified!");
                }

                if (!TypeUtilities.TryParseEnum(ResType, out ResourceType resType))
                {
                        CLIMessageUtilities.Error<ImportResourceCommand>("Error: Invalid resource type specified!");
                        return;
                }

                string resourcesDirectory = pathProvider.GetDirectory(VolatilityPathLocation.Resources);
                string toolsDirectory = pathProvider.GetDirectory(VolatilityPathLocation.Tools);
                string splicerDirectory = pathProvider.GetDirectory(VolatilityPathLocation.Splicer);

                List<Task> tasks = new List<Task>();
                foreach (string sourceFile in sourceFiles)
                {
                        tasks.Add(Task.Run(async () =>
                        {
                                ImportResourceResult result = await importOperation.ExecuteAsync(new ImportResourceRequest(
                                        resType,
                                        platform,
                                        sourceFile,
                                        isX64,
                                        resourcesDirectory,
                                        toolsDirectory,
                                        splicerDirectory,
                                        Overwrite));

                                OperationResult<SaveResourceResult> saveResult = await saveOperation.ExecuteAsync(
                                        new SaveResourceRequest(result.Resource, result.ResourcePath),
                                        progress: null,
                                        cancellationToken: CancellationToken.None);

                                if (!saveResult.Success)
                                {
                                        throw OperationResultFactory.CreateException(saveResult, "Failed to save imported resource.");
                                }

                                CLIMessageUtilities.Success<ImportResourceCommand>(
                                        $"Imported {Path.GetFileName(sourceFile)} as {pathProvider.GetFullPath(result.ResourcePath)}.",
                                        MessageCategory.Resource);
                        }));
                }

                await Task.WhenAll(tasks);
        }

        public void SetArgs(Dictionary<string, object> args)
        {
                ResType = (args.TryGetValue("type", out object? restype) ? restype as string : "auto")?.ToUpper();
                Format = (args.TryGetValue("format", out object? format) ? format as string : "auto")?.ToUpper();
                ImportPath = args.TryGetValue("path", out object? path) ? path as string : "";
                Overwrite = args.TryGetValue("overwrite", out var ow) && (bool)ow;
                Recursive = args.TryGetValue("recurse", out var re) && (bool)re;
        }

    public ImportResourceCommand(
        IPathProvider pathProvider,
        ImportResourceOperation importOperation,
        SaveResourceOperation saveOperation)
    {
        this.pathProvider = pathProvider;
        this.importOperation = importOperation;
        this.saveOperation = saveOperation;
    }
}
