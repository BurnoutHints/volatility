using Volatility.Abstractions.Operations;
using Volatility.Operations;
using Volatility.Resources;
using Volatility.Utilities;
using YamlDotNet.Serialization;

namespace Volatility.Operations.Resources;

internal sealed class SaveResourceOperation : IOperation<SaveResourceRequest, SaveResourceResult>
{
    private readonly ISerializer serializer;

    public SaveResourceOperation()
    {
        serializer = new SerializerBuilder()
            .DisableAliases()
            .WithTypeInspector(inner => new IncludeFieldsTypeInspector(inner))
            .WithTypeConverter(new ResourceYamlTypeConverter())
            .WithTypeConverter(new BitArrayYamlTypeConverter())
            .WithTypeConverter(new StrongIDYamlTypeConverter())
            .WithTypeConverter(new StringEnumYamlTypeConverter())
            .Build();
    }

    public async Task<OperationResult<SaveResourceResult>> ExecuteAsync(
        SaveResourceRequest request,
        IProgress<OperationProgress>? progress,
        CancellationToken cancellationToken)
    {
        if (request.Resource is null)
        {
            return OperationResultFactory.Failure<SaveResourceResult>(
                "save_resource_missing_resource",
                "A resource instance is required.",
                nameof(SaveResourceOperation));
        }

        if (string.IsNullOrWhiteSpace(request.FilePath))
        {
            return OperationResultFactory.Failure<SaveResourceResult>(
                "save_resource_missing_path",
                "A file path is required.",
                nameof(SaveResourceOperation));
        }

        try
        {
            string? directoryPath = Path.GetDirectoryName(request.FilePath);
            if (!string.IsNullOrEmpty(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            string serializedString = serializer.Serialize(request.Resource);
            await File.WriteAllTextAsync(request.FilePath, serializedString, cancellationToken);

            progress?.Report(new OperationProgress("save-resource", 1.0, request.FilePath));
            return OperationResultFactory.Success(new SaveResourceResult(request.FilePath));
        }
        catch (Exception ex)
        {
            return OperationResultFactory.Failure<SaveResourceResult>(
                "save_resource_failed",
                ex.Message,
                nameof(SaveResourceOperation));
        }
    }

}

internal sealed record SaveResourceRequest(Resource Resource, string FilePath) : IOperationRequest;

internal sealed record SaveResourceResult(string FilePath);
