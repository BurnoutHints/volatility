using System.Runtime.Serialization;
using Volatility.Abstractions.Operations;
using Volatility.Operations;
using Volatility.Resources;
using Volatility.Utilities;

namespace Volatility.Operations.Resources;

internal sealed class LoadResourceOperation : IOperation<LoadResourceRequest, LoadResourceResult>
{
    public async Task<OperationResult<LoadResourceResult>> ExecuteAsync(
        LoadResourceRequest request,
        IProgress<OperationProgress>? progress,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.SourceFile))
        {
            return OperationResultFactory.Failure<LoadResourceResult>(
                "load_resource_missing_path",
                "A source file path is required.",
                nameof(LoadResourceOperation));
        }

        try
        {
            string yaml = await File.ReadAllTextAsync(request.SourceFile, cancellationToken);

            Resource resource = ResourceFactory.CreateResource(request.ResourceType, request.Platform);
            Resource? result = (Resource?)ResourceYamlDeserializer.DeserializeResource(resource.GetType(), yaml);

            if (result is null)
            {
                return OperationResultFactory.Failure<LoadResourceResult>(
                    "load_resource_deserialize_failed",
                    $"Unable to deserialize '{Path.GetFileName(request.SourceFile)}'.",
                    nameof(LoadResourceOperation));
            }

            result.ImportedFileName = request.SourceFile;
            progress?.Report(new OperationProgress("load-resource", 1.0, request.SourceFile));
            return OperationResultFactory.Success(new LoadResourceResult(result));
        }
        catch (SerializationException ex)
        {
            return OperationResultFactory.Failure<LoadResourceResult>(
                "load_resource_deserialize_failed",
                ex.Message,
                nameof(LoadResourceOperation));
        }
        catch (Exception ex)
        {
            return OperationResultFactory.Failure<LoadResourceResult>(
                "load_resource_failed",
                ex.Message,
                nameof(LoadResourceOperation));
        }
    }

}

internal sealed record LoadResourceRequest(string SourceFile, ResourceType ResourceType, Platform Platform) : IOperationRequest;

internal sealed record LoadResourceResult(Resource Resource);
