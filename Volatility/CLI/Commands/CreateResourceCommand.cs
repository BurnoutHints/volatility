using Volatility.Operations.Resources;
using Volatility.Resources;
using Volatility.Utilities;

using static Volatility.Utilities.EnvironmentUtilities;
using static Volatility.Utilities.ResourceIDUtilities;

namespace Volatility.CLI.Commands;

internal class CreateResourceCommand : ICommand
{
    public static string CommandToken => "CreateResource";
    public static string CommandDescription => "Creates a new resource file with default/empty fields.";
    public static string CommandParameters => "[--overwrite] --type=<resource type OR index> --format=<tub,bpr[x64],x360,ps3> --name=<asset name> [--id=<resource id>] [--outpath=<file path>]";

    public string? ResType { get; set; }
    public string? Format { get; set; }
    public string? Name { get; set; }
    public string? ResourceId { get; set; }
    public string? OutputPath { get; set; }
    public bool Overwrite { get; set; }

    public async Task Execute()
    {
        if (string.IsNullOrWhiteSpace(ResType))
        {
            Console.WriteLine("Error: No resource type specified! (--type)");
            return;
        }

        if (string.IsNullOrWhiteSpace(Format))
        {
            Console.WriteLine("Error: No format specified! (--format)");
            return;
        }

        if (string.IsNullOrWhiteSpace(Name) && string.IsNullOrWhiteSpace(OutputPath))
        {
            Console.WriteLine("Error: No resource name or output path specified! (--name or --outpath)");
            return;
        }

        string formatValue = Format ?? string.Empty;
        bool isX64 = formatValue.EndsWith("x64", StringComparison.OrdinalIgnoreCase);
        if (isX64)
            formatValue = formatValue[..^3];

        if (!TypeUtilities.TryParseEnum(formatValue, out Platform platform))
        {
            Console.WriteLine("Error: Invalid file format specified!");
            return;
        }

        if (!TypeUtilities.TryParseEnum(ResType, out ResourceType resType))
        {
            Console.WriteLine("Error: Invalid resource type specified!");
            return;
        }

        ResourceID? parsedId = null;
        if (!string.IsNullOrWhiteSpace(ResourceId))
        {
            if (!TryParseResourceID(ResourceId, out var id))
            {
                Console.WriteLine("Error: Invalid resource ID specified! (--id)");
                return;
            }

            parsedId = id;
        }

        string resourcesDirectory = GetEnvironmentDirectory(EnvironmentDirectory.Resources);
        var createOperation = new CreateResourceOperation(resourcesDirectory);
        var saveOperation = new SaveResourceOperation();

        CreateResourceResult result;
        try
        {
            result = createOperation.Execute(resType, platform, Name, OutputPath, parsedId, isX64);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return;
        }

        if (File.Exists(result.ResourcePath) && !Overwrite)
        {
            Console.WriteLine($"Error: Output file already exists ({result.ResourcePath}). Use --overwrite to replace it.");
            return;
        }

        await saveOperation.ExecuteAsync(result.Resource, result.ResourcePath);
        Console.WriteLine($"Created {Path.GetFileName(result.ResourcePath)} at {Path.GetFullPath(result.ResourcePath)}.");
    }

    public void SetArgs(Dictionary<string, object> args)
    {
        ResType = (args.TryGetValue("type", out object? restype) ? restype as string : string.Empty)?.ToUpper();
        Format = (args.TryGetValue("format", out object? format) ? format as string : string.Empty)?.ToUpper();
        Name = args.TryGetValue("name", out object? name) ? name as string : "";
        ResourceId = args.TryGetValue("id", out object? id) ? id as string : "";
        OutputPath = args.TryGetValue("outpath", out object? outpath) ? outpath as string : "";
        Overwrite = args.TryGetValue("overwrite", out var ow) && (bool)ow;
    }

}
