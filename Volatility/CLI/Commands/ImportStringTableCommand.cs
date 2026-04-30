using Volatility.Abstractions.Messaging;
using Volatility.Abstractions.Operations;
using Volatility.Abstractions.Services;
using Volatility.CLI;
using Volatility.Operations.StringTables;

namespace Volatility.CLI.Commands;

internal class ImportStringTableCommand : ICommand
{
    private readonly IPathProvider pathProvider;
    private readonly IStringTableStore stringTableStore;
    private readonly IOperation<LoadResourceDictionaryRequest, LoadResourceDictionaryResult> loadOperation;
    private readonly ImportStringTableOperation importOperation;

    public static string CommandToken => "ImportStringTable";
    public static string CommandDescription => "Imports entries into the ResourceDB from files containing a ResourceStringTable.";
    public static string CommandParameters => "[--verbose] [--overwrite] [--recurse] [--endian=<le,be>] [--version=<v1,v2>] --path=<file path>";

    public string? Endian { get; set; }
    public string? ImportPath { get; set; }
    public string? Version { get; set; }
    public bool Overwrite { get; set; }
    public bool Recursive { get; set; }
    public bool Verbose { get; set; }

    public async Task Execute()
    {
        if (string.IsNullOrEmpty(ImportPath))
        {
            CLIMessageUtilities.Error<ImportStringTableCommand>("Error: No import path specified! (--path)");
            return;
        }

        string[] filePaths = pathProvider.GetFilePaths(ImportPath, VolatilityFilePathFilter.Any, Recursive);
        if (filePaths.Length == 0)
        {
            CLIMessageUtilities.Error<ImportStringTableCommand>("Error: No files or folders found within the specified path!");
            return;
        }

        CLIMessageUtilities.Info<ImportStringTableCommand>(
            "Importing data from ResourceStringTables into the ResourceDB... this may take a while!",
            MessageCategory.StringTable);

        string directoryPath = pathProvider.GetDirectory(VolatilityPathLocation.ResourceDB);
        pathProvider.CreateDirectory(directoryPath);

        string version = Version ?? "v2";

        try
        {
            if (version == "v1")
            {
                string jsonFile = Path.Combine(directoryPath, "ResourceDB.json");
                var importedEntries = new Dictionary<string, Dictionary<string, StringTableResourceEntry>>(StringComparer.OrdinalIgnoreCase);
                await importOperation.ExecuteAsync(filePaths, importedEntries, Endian ?? "le", Overwrite, Verbose);

                var legacyEntries = await stringTableStore.LoadJsonAsync(jsonFile);
                stringTableStore.MergeLegacyEntries(legacyEntries, importedEntries, Overwrite);
                await stringTableStore.WriteJsonAsync(jsonFile, legacyEntries);

                CLIMessageUtilities.Success<ImportStringTableCommand>(
                    $"Finished importing all ResourceDB (v1) data at {jsonFile}.",
                    MessageCategory.StringTable);
                return;
            }

            if (version != "v2")
            {
                CLIMessageUtilities.Error<ImportStringTableCommand>("Error: Invalid version specified! (--version must be v1 or v2)");
                return;
            }

            string yamlFile = Path.Combine(directoryPath, "ResourceDB.yaml");
            OperationResult<LoadResourceDictionaryResult> loadResult = await loadOperation.ExecuteAsync(
                new LoadResourceDictionaryRequest(yamlFile),
                progress: null,
                cancellationToken: CancellationToken.None);
            CLIMessageUtilities.PublishIssues(loadResult.Issues, MessageCategory.StringTable);

            if (!loadResult.Success || loadResult.Value == null)
            {
                return;
            }

            await importOperation.ExecuteAsync(filePaths, loadResult.Value.Entries, Endian ?? "le", Overwrite, Verbose);
            await stringTableStore.WriteYamlAsync(yamlFile, loadResult.Value.Entries);

            CLIMessageUtilities.Success<ImportStringTableCommand>(
                $"Finished importing all ResourceDB (v2) data at {yamlFile}.",
                MessageCategory.StringTable);
        }
        catch (Exception ex)
        {
            CLIMessageUtilities.Error<ImportStringTableCommand>($"Error: {ex.Message}");
        }
    }

    public void SetArgs(Dictionary<string, object> args)
    {
        Endian = (args.TryGetValue("endian", out object? format) ? format as string : "le")?.ToLowerInvariant() ?? "le";
        ImportPath = args.TryGetValue("path", out object? path) ? path as string : "";
        Version = (args.TryGetValue("version", out object? version) ? version as string : "v2")?.ToLowerInvariant() ?? "v2";
        Overwrite = args.TryGetValue("overwrite", out var ow) && (bool)ow;
        Recursive = args.TryGetValue("recurse", out var re) && (bool)re;
        Verbose = args.TryGetValue("verbose", out var ve) && (bool)ve;
    }

    public ImportStringTableCommand(
        IPathProvider pathProvider,
        IStringTableStore stringTableStore,
        IOperation<LoadResourceDictionaryRequest, LoadResourceDictionaryResult> loadOperation,
        ImportStringTableOperation importOperation)
    {
        this.pathProvider = pathProvider;
        this.stringTableStore = stringTableStore;
        this.loadOperation = loadOperation;
        this.importOperation = importOperation;
    }
}
