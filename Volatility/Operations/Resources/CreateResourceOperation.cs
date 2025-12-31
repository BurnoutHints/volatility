using Volatility.Resources;

namespace Volatility.Operations.Resources;

internal sealed class CreateResourceOperation
{
    private readonly string resourcesDirectory;

    public CreateResourceOperation(string resourcesDirectory)
    {
        this.resourcesDirectory = resourcesDirectory;
    }

    public CreateResourceResult Execute(
        ResourceType resourceType,
        Platform platform,
        string? assetName,
        string? outputPath,
        ResourceID? resourceId,
        bool isX64)
    {
        Resource resource = ResourceFactory.CreateResource(resourceType, platform, string.Empty, isX64);

        string resolvedPath = ResolveOutputPath(outputPath, resourceType, assetName);
        string resolvedAssetName = ResolveAssetName(assetName, resolvedPath);

        if (!string.IsNullOrWhiteSpace(resolvedAssetName))
            resource.AssetName = resolvedAssetName;

        if (resourceId.HasValue)
        {
            resource.ResourceID = resourceId.Value;
        }
        else if (!string.IsNullOrWhiteSpace(resolvedAssetName))
        {
            resource.ResourceID = ResourceID.HashFromString(resolvedAssetName);
        }

        if (resource is ShaderBase shader && string.IsNullOrWhiteSpace(shader.ShaderSourcePath))
        {
            shader.ShaderSourcePath = $"{Path.GetFileName(resolvedPath)}.hlsl";
        }

        return new CreateResourceResult(resource, resolvedPath);
    }

    private string ResolveOutputPath(string? outputPath, ResourceType resourceType, string? assetName)
    {
        string resolvedPath;

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            if (string.IsNullOrWhiteSpace(assetName))
                throw new InvalidOperationException("Either a resource name or an output path must be provided.");

            resolvedPath = Path.Combine(resourcesDirectory, NormalizeGameDbPath(assetName));
        }
        else if (!Path.IsPathRooted(outputPath))
        {
            resolvedPath = Path.Combine(resourcesDirectory, NormalizeGameDbPath(outputPath));
        }
        else
        {
            resolvedPath = NormalizeGameDbPath(outputPath);
        }

        return Path.ChangeExtension(resolvedPath, resourceType.ToString());
    }

    private static string ResolveAssetName(string? assetName, string outputPath)
    {
        if (!string.IsNullOrWhiteSpace(assetName))
            return assetName;

        return Path.GetFileNameWithoutExtension(outputPath);
    }

    private static string NormalizeGameDbPath(string path)
    {
        const string prefix = "gamedb://";

        if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return "gamedb/" + path[prefix.Length..];

        return path;
    }
}

internal sealed record CreateResourceResult(Resource Resource, string ResourcePath);
