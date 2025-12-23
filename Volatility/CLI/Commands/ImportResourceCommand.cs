using Volatility.Operations.Resources;
using Volatility.Resources;
using Volatility.Utilities;

using static Volatility.Utilities.EnvironmentUtilities;

namespace Volatility.CLI.Commands;

internal class ImportResourceCommand : ICommand
{
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
                        Console.WriteLine("Error: Automatic typing is not supported yet! Please specify a type (--type)");
                        return;
                }

                if (string.IsNullOrEmpty(ImportPath))
                {
                        Console.WriteLine("Error: No import path specified! (--path)");
                        return;
                }

                try
                {
                        File.GetAttributes(ImportPath);
                }
                catch (FileNotFoundException)
                {
                        Console.WriteLine("Error: Invalid file import path specified!");
                        return;
                }
                catch (DirectoryNotFoundException)
                {
                        Console.WriteLine("Error: Can not find directory for specified import path!");
                        return;
                }
                catch (Exception e)
                {
                        Console.WriteLine($"Error: Caught file exception: {e.Message}");
                        return;
                }

                string[] sourceFiles = ICommand.GetFilePathsInDirectory(ImportPath, ICommand.TargetFileType.Header, Recursive);

                if (sourceFiles.Length == 0)
                {
                        Console.WriteLine($"Error: No valid file(s) found at the specified path ({ImportPath}). Ensure the path exists and spaces are properly enclosed. (--path)");
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
                        Console.WriteLine("Error: Invalid resource type specified!");
                        return;
                }

                string resourcesDirectory = GetEnvironmentDirectory(EnvironmentDirectory.Resources);
                string toolsDirectory = GetEnvironmentDirectory(EnvironmentDirectory.Tools);
                string splicerDirectory = GetEnvironmentDirectory(EnvironmentDirectory.Splicer);

                var importOperation = new ImportResourceOperation(resourcesDirectory, toolsDirectory, splicerDirectory, Overwrite);
                var saveOperation = new SaveResourceOperation();

                List<Task> tasks = new List<Task>();
                foreach (string sourceFile in sourceFiles)
                {
                        tasks.Add(Task.Run(async () =>
                        {
                                ImportResourceResult result = await importOperation.ExecuteAsync(resType, platform, sourceFile, isX64);
                                await saveOperation.ExecuteAsync(result.Resource, result.ResourcePath);
                                Console.WriteLine($"Imported {Path.GetFileName(sourceFile)} as {Path.GetFullPath(result.ResourcePath)}.");
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
    public ImportResourceCommand() { }
}
