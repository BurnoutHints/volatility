using System.Text.RegularExpressions;
using Volatility.Abstractions.Messaging;
using Volatility.Abstractions.Operations;
using Volatility.Abstractions.Services;
using Volatility.Operations;
using Volatility.Resources;

namespace Volatility.Operations.Resources;

internal sealed partial class ImportResourceOperation(
    IResourceSerializer resourceSerializer,
    IResourceDBLookup resourceDBLookup,
    ITextureBitmapStore textureBitmapStore,
    IProcessRunner processRunner,
    IShaderSourceStore shaderSourceStore,
    IMessageSink messageSink)
    : IOperation<ImportResourceRequest, ImportResourceResult>
{
    public async Task<OperationResult<ImportResourceResult>> ExecuteAsync(
        ImportResourceRequest request,
        IProgress<OperationProgress>? progress,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            Resource resource;
            using (FileStream stream = File.OpenRead(request.SourceFile))
            {
                resource = resourceSerializer.Deserialize(
                    stream,
                    request.ResourceType,
                    request.Platform,
                    new ResourceSerializationOptions
                    {
                        FileName = request.SourceFile,
                        ResourceDBLookup = resourceDBLookup,
                        x64 = request.IsX64
                    });
            }

            string filePath = Path.Combine
            (
                request.ResourcesDirectory,
                $"{DBToFileRegex().Replace(resource.AssetName, string.Empty)}.{request.ResourceType}"
            );

            string? directoryPath = Path.GetDirectoryName(filePath);

            if (!string.IsNullOrEmpty(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            if (resource is ShaderBase shader)
            {
                shaderSourceStore.MaterializeImportedSource(shader, request.ResourcesDirectory);
            }

            if (request.ResourceType == ResourceType.Texture)
            {
                string texturePath = textureBitmapStore.GetSecondaryBitmapPath(request.SourceFile, resource.Unpacker);

                if (resource is TextureBase texture && File.Exists(texturePath))
                {
                    string outPath = Path.Combine
                    (
                        directoryPath ?? string.Empty,
                        Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(Path.GetFullPath(filePath)))
                    );

                    textureBitmapStore.WriteNormalizedBitmapFile(texture, texturePath, $"{outPath}.{request.ResourceType}Bitmap", request.Overwrite);
                }
            }

            if (request.ResourceType == ResourceType.Splicer)
            {
                string sxPath = Path.Combine
                (
                    request.ToolsDirectory,
                    "sx.exe"
                );

                bool sxExists = File.Exists(sxPath);

                Splicer? splicer = resource as Splicer;

                List<Splicer.SpliceSample>? samples = splicer?.GetLoadedSamples();

                string sampleDirectory = Path.Combine
                (
                    request.SplicerDirectory,
                    "Samples"
                );

                Directory.CreateDirectory(sampleDirectory);

                if (samples != null)
                {
                    for (int i = 0; i < samples.Count; i++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        string sampleName = $"{samples[i].SampleID}";

                        string samplePathName = Path.Combine(sampleDirectory, sampleName);

                        if (!File.Exists($"{samplePathName}.snr") || request.Overwrite)
                        {
                            messageSink.Info(
                                $"Writing extracted sample {sampleName}.snr",
                                MessageCategory.Resource,
                                nameof(ImportResourceOperation));
                            await File.WriteAllBytesAsync($"{samplePathName}.snr", samples[i].Data, cancellationToken);
                        }
                        else
                        {
                            messageSink.Info(
                                $"Skipping extracted sample {sampleName}.snr",
                                MessageCategory.Resource,
                                nameof(ImportResourceOperation));
                        }

                        if (sxExists)
                        {
                            string convertedSamplePathName = Path.Combine(sampleDirectory, "_extracted");

                            Directory.CreateDirectory(convertedSamplePathName);

                            convertedSamplePathName = Path.Combine(convertedSamplePathName, sampleName + ".wav");

                            if (!File.Exists(convertedSamplePathName) || request.Overwrite)
                            {
                                messageSink.Info(
                                    $"Converting extracted sample {sampleName}.snr to wave...",
                                    MessageCategory.Resource,
                                    nameof(ImportResourceOperation));
                                processRunner.RunAndRelayOutput(
                                    sxPath,
                                    $"-wave -s16l_int -v0 \"{samplePathName}.snr\" -=\"{convertedSamplePathName}\"");
                            }
                            else
                            {
                                messageSink.Info(
                                    $"Converted sample {Path.GetFileName(convertedSamplePathName)} already exists, skipping...",
                                    MessageCategory.Resource,
                                    nameof(ImportResourceOperation));
                            }
                        }
                    }
                }
            }

            progress?.Report(new OperationProgress("import-resource", 1.0, filePath));
            return OperationResultFactory.Success(new ImportResourceResult(resource, filePath));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return OperationResultFactory.Failure<ImportResourceResult>(
                "import_resource_failed",
                ex.Message,
                nameof(ImportResourceOperation));
        }
    }

    [GeneratedRegex(@"(\?ID=\d+)|:")]
    private static partial Regex DBToFileRegex();
}

public sealed record ImportResourceRequest(
    ResourceType ResourceType,
    Platform Platform,
    string SourceFile,
    bool IsX64,
    string ResourcesDirectory,
    string ToolsDirectory,
    string SplicerDirectory,
    bool Overwrite) : IOperationRequest;

public sealed record ImportResourceResult(Resource Resource, string ResourcePath);
