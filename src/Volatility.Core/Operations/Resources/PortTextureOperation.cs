using System.Reflection;

using Volatility.Abstractions.Messaging;
using Volatility.Abstractions.Operations;
using Volatility.Abstractions.Services;
using Volatility.Operations;
using Volatility.Resources;
using Volatility.Utilities;

using static Volatility.Utilities.ResourceIDUtilities;

namespace Volatility.Operations.Resources;

internal sealed class PortTextureOperation(
    IResourceFactory resourceFactory,
    IResourceDBLookup resourceDBLookup,
    ITextureBitmapStore textureBitmapStore,
    IMessageSink messageSink)
    : IOperation<PortTextureRequest, PortTextureResult>
{
    public async Task<OperationResult<PortTextureResult>> ExecuteAsync(
        PortTextureRequest request,
        IProgress<OperationProgress>? progress,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            string resolvedDestinationPath = string.IsNullOrEmpty(request.DestinationPath)
                ? request.SourcePath
                : request.DestinationPath;
            TextureFormatSpec sourceSpec = ParseTextureFormat(request.SourceFormat);
            TextureFormatSpec destinationSpec = ParseTextureFormat(request.DestinationFormat);

            List<string> outputPaths = new(request.SourceFiles.Count);
            for (int i = 0; i < request.SourceFiles.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string outputPath = await PortFileAsync(
                    request.SourceFiles[i],
                    resolvedDestinationPath,
                    sourceSpec,
                    destinationSpec,
                    request.Verbose,
                    request.UseGTF,
                    cancellationToken);
                outputPaths.Add(outputPath);
                progress?.Report(new OperationProgress(
                    "port-texture",
                    (double)outputPaths.Count / request.SourceFiles.Count,
                    outputPath));
            }

            return OperationResultFactory.Success(new PortTextureResult(outputPaths));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return OperationResultFactory.Failure<PortTextureResult>(
                "port_texture_failed",
                ex.Message,
                nameof(PortTextureOperation));
        }
    }

    private async Task<string> PortFileAsync(
        string sourceFile,
        string resolvedDestinationPath,
        TextureFormatSpec sourceSpec,
        TextureFormatSpec destinationSpec,
        bool verbose,
        bool useGTF,
        CancellationToken cancellationToken)
    {
        TextureBase sourceTexture = LoadSourceTexture(sourceFile, sourceSpec, verbose);
        TextureBase destinationTexture = CreateDestinationTexture(destinationSpec, verbose);

        string localSourceFormat = sourceSpec.DisplayName;
        string localDestinationFormat = destinationSpec.DisplayName;

        TextureFormatConverter.CopyProperties(sourceTexture, destinationTexture);

        TextureFormatConverter.ConvertFormat(
            sourceTexture,
            destinationTexture,
            localSourceFormat,
            localDestinationFormat,
            out bool flipEndian,
            out int sourceFormatIndex,
            out int destinationFormatIndex,
            msg => LogWarning(msg));

        destinationTexture.PushAll();

        string outPath = string.Empty;

        string outResourceFilename = (flipEndian && sourceTexture.Unpacker != Unpacker.YAP)
            ? FlipPathResourceIDEndian(Path.GetFileName(sourceFile))
            : Path.GetFileName(sourceFile);

        if (resolvedDestinationPath == sourceFile)
        {
            outPath = $"{Path.GetDirectoryName(resolvedDestinationPath)}{Path.DirectorySeparatorChar}{outResourceFilename}";
        }
        else if (Directory.Exists(resolvedDestinationPath) || !Path.HasExtension(resolvedDestinationPath))
        {
            outPath = Path.Combine(resolvedDestinationPath, outResourceFilename);
        }
        else
        {
            outPath = resolvedDestinationPath;
        }

        string? outputDirectory = Path.GetDirectoryName(outPath);
        if (!string.IsNullOrEmpty(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        string sourceBitmapPath = textureBitmapStore.GetSecondaryBitmapPath(sourceFile, sourceTexture.Unpacker);

        if (!Path.Exists(sourceBitmapPath))
        {
            LogWarning($"Failed to find associated bitmap data for {Path.GetFileNameWithoutExtension(sourceFile)} at path {sourceBitmapPath}!");
        }

        string destinationBitmapPath = textureBitmapStore.GetSecondaryBitmapPath(outPath, sourceTexture.Unpacker);

        if (Path.Exists(destinationBitmapPath))
        {
            LogVerbose(verbose, $"Found existing bitmap data at {destinationBitmapPath}, overwriting...");
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (useGTF && sourceTexture is TexturePS3 sourcePs3)
            {
                textureBitmapStore.ConvertPS3GTFToDDS(sourcePs3, sourceBitmapPath, destinationBitmapPath, verbose);
            }

            byte[] sourceBitmapData = textureBitmapStore.ReadNormalizedBitmapData(sourceTexture, sourceBitmapPath);

            if (destinationTexture is TextureX360 destX && sourceTexture.ResourcePlatform != Platform.X360)
            {
                destX.Format.MaxMipLevel = destX.Format.MinMipLevel;
            }

            if (!TextureFormatConverter.TryConvertTexture(sourceTexture, destinationTexture, sourceBitmapData, destinationBitmapPath))
            {
                LogVerbose(verbose, $"Writing associated bitmap data for {Path.GetDirectoryName(outPath)}{Path.DirectorySeparatorChar}{Path.GetFileNameWithoutExtension(outPath)}_texture.dat...");
                await File.WriteAllBytesAsync(destinationBitmapPath, sourceBitmapData, cancellationToken);
            }
            else
            {
                LogVerbose(verbose, $"Converting associated bitmap data for {Path.GetDirectoryName(outPath)}{Path.DirectorySeparatorChar}{Path.GetFileNameWithoutExtension(outPath)}_texture.dat...");
            }
            LogVerbose(verbose, $"Wrote texture bitmap data to {destinationSpec.DisplayName} destination directory.");

            if (destinationTexture is TextureBPR destBprTexture && File.Exists(destinationBitmapPath))
            {
                destBprTexture.PlacedDataSize = (uint)new FileInfo(destinationBitmapPath).Length;
                LogVerbose(verbose, $"BPR PlacedDataSize set to {destBprTexture.PlacedDataSize} (file: {destinationBitmapPath}).");
            }

            using FileStream fs = new(outPath, FileMode.Create, FileAccess.Write);
            using (ResourceBinaryWriter writer = new(fs, destinationTexture.ResourceEndian))
            {
                try
                {
                    LogVerbose(verbose, $"Writing converted {destinationSpec.DisplayName} texture property data to destination file {Path.GetFileName(outPath)}...");
                    destinationTexture.WriteToStream(writer);
                }
                catch
                {
                    throw new IOException("Failed to write converted texture property data to stream.");
                }
                writer.Close();
                fs.Close();
            }
        }
        catch (Exception ex)
        {
            throw new IOException(
                $"Failed to port texture data for {Path.GetFileNameWithoutExtension(sourceFile)}: {ex.Message}",
                ex);
        }

        messageSink.Success(
            $"Successfully ported {localSourceFormat} formatted {Path.GetFileNameWithoutExtension(sourceFile)} to {localDestinationFormat} as {Path.GetFileNameWithoutExtension(outPath)}.",
            MessageCategory.Texture,
            nameof(PortTextureOperation));
        return outPath;
    }

    private TextureBase LoadSourceTexture(string path, TextureFormatSpec format, bool verbose)
    {
        LogVerbose(verbose, $"Loading {format.DisplayName} texture property data...");
        return (TextureBase)resourceFactory.LoadResource(ResourceType.Texture, format.Platform, path, resourceDBLookup, format.IsX64);
    }

    private TextureBase CreateDestinationTexture(TextureFormatSpec format, bool verbose)
    {
        LogVerbose(verbose, $"Constructing {format.DisplayName} texture property data...");
        return (TextureBase)resourceFactory.CreateResource(ResourceType.Texture, format.Platform, format.IsX64);
    }

    private void LogVerbose(bool verbose, string text)
    {
        if (verbose)
        {
            messageSink.Verbose(text, MessageCategory.Texture, nameof(PortTextureOperation));
        }
    }

    private void LogWarning(string text)
    {
        messageSink.Warning(text, MessageCategory.Texture, nameof(PortTextureOperation));
    }

    private static TextureFormatSpec ParseTextureFormat(string format)
    {
        string normalizedFormat = format.Trim().ToUpperInvariant();
        bool isX64 = normalizedFormat.EndsWith("X64", StringComparison.OrdinalIgnoreCase);
        if (isX64)
        {
            normalizedFormat = normalizedFormat[..^3];
        }

        Platform platform = normalizedFormat switch
        {
            "BPR" => Platform.BPR,
            "TUB" => Platform.TUB,
            "X360" => Platform.X360,
            "PS3" => Platform.PS3,
            _ => throw new InvalidPlatformException(),
        };

        return new TextureFormatSpec(platform, isX64, isX64 ? $"{normalizedFormat}X64" : normalizedFormat);
    }

    private readonly record struct TextureFormatSpec(Platform Platform, bool IsX64, string DisplayName);
}

public sealed record PortTextureRequest(
    IReadOnlyList<string> SourceFiles,
    string SourceFormat,
    string SourcePath,
    string DestinationFormat,
    string? DestinationPath,
    bool Verbose,
    bool UseGTF) : IOperationRequest;

public sealed record PortTextureResult(IReadOnlyList<string> OutputPaths);
