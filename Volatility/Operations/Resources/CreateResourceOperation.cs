using Volatility.Abstractions.Operations;
using Volatility.Operations;
using Volatility.Resources;

namespace Volatility.Operations.Resources;

internal sealed class CreateResourceOperation(string resourcesDirectory)
    : IOperation<CreateResourceRequest, CreateResourceResult>
{
    public Task<OperationResult<CreateResourceResult>> ExecuteAsync(
        CreateResourceRequest request,
        IProgress<OperationProgress>? progress,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            Resource resource = ResourceFactory.CreateResource(request.ResourceType, request.Platform, request.IsX64);

            string resolvedPath = ResolveOutputPath(request.OutputPath, request.ResourceType, request.AssetName);
            string resolvedAssetName = ResolveAssetName(request.AssetName, resolvedPath);

            if (!string.IsNullOrWhiteSpace(resolvedAssetName))
            {
                resource.AssetName = resolvedAssetName;
            }

            if (request.ResourceId.HasValue)
            {
                resource.ResourceID = request.ResourceId.Value;
            }
            else if (!string.IsNullOrWhiteSpace(resolvedAssetName))
            {
                resource.ResourceID = ResourceID.HashFromString(resolvedAssetName);
            }

            if (resource is ShaderBase shader && string.IsNullOrWhiteSpace(shader.ShaderSourcePath))
            {
                shader.ShaderSourcePath = $"{Path.GetFileName(resolvedPath)}.hlsl";
            }

            progress?.Report(new OperationProgress("create-resource", 1.0, resolvedPath));
            return Task.FromResult(OperationResultFactory.Success(new CreateResourceResult(resource, resolvedPath)));
        }
        catch (Exception ex)
        {
            return Task.FromResult(OperationResultFactory.Failure<CreateResourceResult>(
                "create_resource_failed",
                ex.Message,
                nameof(CreateResourceOperation)));
        }
    }

    private string ResolveOutputPath(string? outputPath, ResourceType resourceType, string? assetName)
    {
        string resolvedPath;

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            if (string.IsNullOrWhiteSpace(assetName))
            {
                throw new InvalidOperationException("Either a resource name or an output path must be provided.");
            }

            resolvedPath = Path.Combine(resourcesDirectory, NormalizeGameDBPath(assetName));
        }
        else if (!Path.IsPathRooted(outputPath))
        {
            resolvedPath = Path.Combine(resourcesDirectory, NormalizeGameDBPath(outputPath));
        }
        else
        {
            resolvedPath = NormalizeGameDBPath(outputPath);
        }

        return Path.ChangeExtension(resolvedPath, resourceType.ToString());
    }

    private static string ResolveAssetName(string? assetName, string outputPath)
    {
        if (!string.IsNullOrWhiteSpace(assetName))
        {
            return assetName;
        }

        return Path.GetFileNameWithoutExtension(outputPath);
    }

    private static string NormalizeGameDBPath(string path)
    {
        const string prefix = "gamedb://";

        if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return "gamedb/" + path[prefix.Length..];
        }

        return path;
    }
}

internal sealed record CreateResourceRequest(
    ResourceType ResourceType,
    Platform Platform,
    string? AssetName,
    string? OutputPath,
    ResourceID? ResourceId,
    bool IsX64) : IOperationRequest;

internal sealed record CreateResourceResult(Resource Resource, string ResourcePath);
