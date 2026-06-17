using Volatility.Abstractions.Messaging;
using Volatility.Abstractions.Operations;
using Volatility.Abstractions.Services;
using Volatility.Operations;
using Volatility.Resources;
using Volatility.Utilities;

namespace Volatility.Operations.Resources;

internal sealed class TextureToDDSOperation(
    IResourceSerializer resourceSerializer,
    IResourceDBLookup resourceDBLookup,
    ITextureBitmapStore textureBitmapStore,
    IMessageSink messageSink)
    : IOperation<TextureToDDSRequest, TextureToDDSResult>
{
    public async Task<OperationResult<TextureToDDSResult>> ExecuteAsync(
        TextureToDDSRequest request,
        IProgress<OperationProgress>? progress,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            string[] files = request.SourceFiles.ToArray();
            bool multipleInputs = files.Length > 1;

            List<Task<string>> tasks = new();
            foreach (string sourceFile in files)
            {
                tasks.Add(ConvertFileAsync(sourceFile, request, multipleInputs, cancellationToken));
            }

            string[] outputPaths = await Task.WhenAll(tasks);
            progress?.Report(new OperationProgress("texture-to-dds", 1.0, null));
            return OperationResultFactory.Success(new TextureToDDSResult(outputPaths));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return OperationResultFactory.Failure<TextureToDDSResult>(
                "texture_to_dds_failed",
                ex.Message,
                nameof(TextureToDDSOperation));
        }
    }

    public static byte[] ConvertToDDS(TextureBase texture, byte[] bitmapData)
    {
        return DDSTextureUtilities.CreateDDSFile(texture, bitmapData);
    }

    private async Task<string> ConvertFileAsync(
        string sourceFile,
        TextureToDDSRequest request,
        bool multipleInputs,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using FileStream stream = File.OpenRead(sourceFile);
        TextureBase texture = (TextureBase)resourceSerializer.Deserialize(
            stream,
            ResourceType.Texture,
            request.Platform,
            new ResourceSerializationOptions
            {
                FileName = sourceFile,
                ResourceDBLookup = resourceDBLookup,
                x64 = request.IsX64
            });

        string sourceBitmapPath = textureBitmapStore.GetSecondaryBitmapPath(sourceFile, texture.Unpacker);

        if (!File.Exists(sourceBitmapPath))
        {
            throw new FileNotFoundException($"Failed to find associated bitmap data at path '{sourceBitmapPath}'.");
        }

        byte[] bitmapData = textureBitmapStore.ReadNormalizedBitmapData(texture, sourceBitmapPath);
        byte[] ddsData = ConvertToDDS(texture, bitmapData);
        string destinationPath = ResolveOutputPath(sourceFile, texture.Unpacker, request.OutputPath, multipleInputs, textureBitmapStore);

        string? destinationDirectory = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrEmpty(destinationDirectory))
        {
            Directory.CreateDirectory(destinationDirectory);
        }

        if (!request.Overwrite && File.Exists(destinationPath))
        {
            throw new IOException($"The file '{destinationPath}' already exists.");
        }

        if (request.Verbose)
        {
            messageSink.Verbose(
                $"Writing DDS texture data to {destinationPath}...",
                MessageCategory.Texture,
                nameof(TextureToDDSOperation));
        }

        await File.WriteAllBytesAsync(destinationPath, ddsData, cancellationToken);
        messageSink.Info(
            $"Wrote DDS for {Path.GetFileName(sourceFile)} to {destinationPath}.",
            MessageCategory.Texture,
            nameof(TextureToDDSOperation));

        return destinationPath;
    }

    private static string ResolveOutputPath(
        string sourceFile,
        Unpacker unpacker,
        string? outputPath,
        bool multipleInputs,
        ITextureBitmapStore textureBitmapStore)
    {
        string outputName = textureBitmapStore.GetResourceBaseName(sourceFile, unpacker) + ".dds";

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            return Path.Combine(Path.GetDirectoryName(sourceFile) ?? string.Empty, outputName);
        }

        bool outputLooksLikeFile = Path.HasExtension(outputPath)
            && string.Equals(Path.GetExtension(outputPath), ".dds", StringComparison.OrdinalIgnoreCase);

        if (!multipleInputs && outputLooksLikeFile)
        {
            return outputPath;
        }

        return Path.Combine(outputPath, outputName);
    }
}

public sealed record TextureToDDSRequest(
    IReadOnlyList<string> SourceFiles,
    Platform Platform,
    bool IsX64,
    string? OutputPath,
    bool Overwrite,
    bool Verbose) : IOperationRequest;

public sealed record TextureToDDSResult(IReadOnlyList<string> OutputPaths);
