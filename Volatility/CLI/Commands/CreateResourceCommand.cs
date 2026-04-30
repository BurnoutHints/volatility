using Volatility.Abstractions.Messaging;
using Volatility.Abstractions.Operations;
using Volatility.Abstractions.Services;
using Volatility.CLI;
using Volatility.Operations.Resources;
using Volatility.Resources;

using static Volatility.Utilities.ResourceIDUtilities;

namespace Volatility.CLI.Commands;

internal class CreateResourceCommand : ICommand
{
    private readonly IPathProvider pathProvider;
    private readonly IOperation<CreateResourceRequest, CreateResourceResult> createOperation;
    private readonly IOperation<SaveResourceRequest, SaveResourceResult> saveOperation;

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
            CLIMessageUtilities.Error<CreateResourceCommand>("Error: No resource type specified! (--type)");
            return;
        }

        if (string.IsNullOrWhiteSpace(Format))
        {
            CLIMessageUtilities.Error<CreateResourceCommand>("Error: No format specified! (--format)");
            return;
        }

        if (string.IsNullOrWhiteSpace(Name) && string.IsNullOrWhiteSpace(OutputPath))
        {
            CLIMessageUtilities.Error<CreateResourceCommand>("Error: No resource name or output path specified! (--name or --outpath)");
            return;
        }

        string formatValue = Format ?? string.Empty;
        bool isX64 = formatValue.EndsWith("x64", StringComparison.OrdinalIgnoreCase);
        if (isX64)
        {
            formatValue = formatValue[..^3];
        }

        if (!Volatility.Utilities.TypeUtilities.TryParseEnum(formatValue, out Platform platform))
        {
            CLIMessageUtilities.Error<CreateResourceCommand>("Error: Invalid file format specified!");
            return;
        }

        if (!Volatility.Utilities.TypeUtilities.TryParseEnum(ResType, out ResourceType resourceType))
        {
            CLIMessageUtilities.Error<CreateResourceCommand>("Error: Invalid resource type specified!");
            return;
        }

        ResourceID? parsedId = null;
        if (!string.IsNullOrWhiteSpace(ResourceId))
        {
            if (!TryParseResourceID(ResourceId, out ResourceID id))
            {
                CLIMessageUtilities.Error<CreateResourceCommand>("Error: Invalid resource ID specified! (--id)");
                return;
            }

            parsedId = id;
        }

        OperationResult<CreateResourceResult> createResult = await createOperation.ExecuteAsync(
            new CreateResourceRequest(resourceType, platform, Name, OutputPath, parsedId, isX64),
            progress: null,
            cancellationToken: CancellationToken.None);
        CLIMessageUtilities.PublishIssues(createResult.Issues, MessageCategory.Resource);

        if (!createResult.Success || createResult.Value == null)
        {
            return;
        }

        CreateResourceResult result = createResult.Value;
        if (pathProvider.FileExists(result.ResourcePath) && !Overwrite)
        {
            CLIMessageUtilities.Error<CreateResourceCommand>($"Error: Output file already exists ({result.ResourcePath}). Use --overwrite to replace it.");
            return;
        }

        OperationResult<SaveResourceResult> saveResult = await saveOperation.ExecuteAsync(
            new SaveResourceRequest(result.Resource, result.ResourcePath),
            progress: null,
            cancellationToken: CancellationToken.None);
        CLIMessageUtilities.PublishIssues(saveResult.Issues, MessageCategory.Resource);

        if (!saveResult.Success)
        {
            return;
        }

        CLIMessageUtilities.Success<CreateResourceCommand>(
            $"Created {Path.GetFileName(result.ResourcePath)} at {pathProvider.GetFullPath(result.ResourcePath)}.",
            MessageCategory.Resource);
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

    public CreateResourceCommand(
        IPathProvider pathProvider,
        IOperation<CreateResourceRequest, CreateResourceResult> createOperation,
        IOperation<SaveResourceRequest, SaveResourceResult> saveOperation)
    {
        this.pathProvider = pathProvider;
        this.createOperation = createOperation;
        this.saveOperation = saveOperation;
    }
}
