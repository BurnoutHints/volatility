using Volatility.Abstractions.Operations;
using Volatility.Operations;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Volatility.Operations.StringTables;

internal sealed class LoadResourceDictionaryOperation
    : IOperation<LoadResourceDictionaryRequest, LoadResourceDictionaryResult>
{
    private readonly IDeserializer deserializer;

    public LoadResourceDictionaryOperation()
    {
        deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
    }

    public async Task<OperationResult<LoadResourceDictionaryResult>> ExecuteAsync(
        LoadResourceDictionaryRequest request,
        IProgress<OperationProgress>? progress,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.YamlFile))
        {
            return OperationResultFactory.Failure<LoadResourceDictionaryResult>(
                "load_resource_dictionary_missing_path",
                "A ResourceDB YAML file path is required.",
                nameof(LoadResourceDictionaryOperation));
        }

        try
        {
            if (!File.Exists(request.YamlFile))
            {
                return OperationResultFactory.Success(
                    new LoadResourceDictionaryResult(
                        new Dictionary<string, Dictionary<string, StringTableResourceEntry>>(StringComparer.OrdinalIgnoreCase)));
            }

            string content = await File.ReadAllTextAsync(request.YamlFile, cancellationToken);
            Dictionary<string, Dictionary<string, StringTableResourceEntry>>? result =
                deserializer.Deserialize<Dictionary<string, Dictionary<string, StringTableResourceEntry>>>(content);

            progress?.Report(new OperationProgress("load-resource-dictionary", 1.0, request.YamlFile));
            return OperationResultFactory.Success(
                new LoadResourceDictionaryResult(
                    result ?? new Dictionary<string, Dictionary<string, StringTableResourceEntry>>(StringComparer.OrdinalIgnoreCase)));
        }
        catch (Exception ex)
        {
            return OperationResultFactory.Failure<LoadResourceDictionaryResult>(
                "load_resource_dictionary_failed",
                ex.Message,
                nameof(LoadResourceDictionaryOperation));
        }
    }

}

public sealed record LoadResourceDictionaryRequest(string YamlFile) : IOperationRequest;

public sealed record LoadResourceDictionaryResult(
    Dictionary<string, Dictionary<string, StringTableResourceEntry>> Entries);
