using Volatility.Operations.StringTables;

using static Volatility.Utilities.EnvironmentUtilities;

namespace Volatility.CLI.Commands;

internal class ImportStringTableCommand : ICommand
{
    public static string CommandToken => "ImportStringTable";
    public static string CommandDescription => "Imports entries into the ResourceDB from files containing a ResourceStringTable.";
    public static string CommandParameters => "[--verbose] [--endian=<le,be>] --path=<file path>";

    public string? Endian { get; set; }
    public string? ImportPath { get; set; }
    public bool Overwrite { get; set; }
    public bool Recursive { get; set; }
    public bool Verbose { get; set; }

    public async Task Execute()
    {
        if (string.IsNullOrEmpty(ImportPath))
        {
            Console.WriteLine("Error: No import path specified! (--path)");
            return;
        }

        var filePaths = ICommand.GetFilePathsInDirectory(ImportPath, ICommand.TargetFileType.Any, Recursive);
        if (filePaths.Length == 0)
        {
            Console.WriteLine("Error: No files or folders found within the specified path!");
            return;
        }

        Console.WriteLine("Importing data from ResourceStringTables into the ResourceDB... this may take a while!");

        string directoryPath = GetEnvironmentDirectory(EnvironmentDirectory.ResourceDB);
        Directory.CreateDirectory(directoryPath);
        string yamlFile = Path.Combine(directoryPath, "ResourceDB.yaml");

        var loadOperation = new LoadResourceDictionaryOperation();
        var mergeOperation = new MergeStringTableEntriesOperation();
        var importOperation = new ImportStringTableOperation(mergeOperation);

        var allEntries = await loadOperation.ExecuteAsync(yamlFile);

        await importOperation.ExecuteAsync(filePaths, allEntries, Endian ?? "le", Overwrite, Verbose, yamlFile);

        Console.WriteLine($"Finished importing all ResourceDB (v2) data at {yamlFile}.");
    }

    public void SetArgs(Dictionary<string, object> args)
    {
        Endian = (args.TryGetValue("endian", out object? format) ? format as string : "le").ToLower();
        ImportPath = args.TryGetValue("path", out object? path) ? path as string : "";
        Overwrite = args.TryGetValue("overwrite", out var ow) && (bool)ow;
        Recursive = args.TryGetValue("recurse", out var re) && (bool)re;
        Verbose = args.TryGetValue("verbose", out var ve) && (bool)ve;
    }

    public ImportStringTableCommand() { }
}
