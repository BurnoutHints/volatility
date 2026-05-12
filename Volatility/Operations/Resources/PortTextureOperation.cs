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

                CopyProperties(sourceTexture, destinationTexture);

                bool flipEndian = false;
                int sourceFormatIndex = 0;
                int destinationFormatIndex = 0;
                switch ((sourceTexture, destinationTexture))
                {
                    case (TexturePS3 ps3, TextureX360 x360):
                        PS3toX360Mapping.TryGetValue(ps3.Format, out GPUTEXTUREFORMAT ps3x360Format);
                        x360.Format.DataFormat = ps3x360Format;
                        x360.Format.Endian = GPUENDIAN.GPUENDIAN_NONE;
                        flipEndian = false;
                        sourceFormatIndex = (int)ps3.Format;
                        destinationFormatIndex = (int)ps3x360Format;
                        if (ps3x360Format == GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_1_REVERSE)
                            LogWarning($"Destination texture format is {ps3x360Format}! (Source is {ps3.Format})");
                        break;
                    case (TextureX360 x360, TexturePS3 ps3):
                        X360toPS3Mapping.TryGetValue(x360.Format.DataFormat, out CELL_GCM_COLOR_FORMAT x360ps3Format);
                        ps3.Format = x360ps3Format;
                        flipEndian = false;
                        sourceFormatIndex = (int)x360.Format.DataFormat;
                        destinationFormatIndex = (int)x360ps3Format;
                        if (x360ps3Format == CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_INVALID)
                            LogWarning($"Destination texture format is {x360ps3Format}! (Source is {x360.Format.DataFormat})");
                        break;
                    case (TextureBPR bprsrc, TextureBPR bprdst):
                        bprdst.Format = bprsrc.Format;
                        sourceFormatIndex = (int)bprsrc.Format;
                        destinationFormatIndex = sourceFormatIndex;
                        break;
                    case (TexturePC tub, TextureBPR bpr):
                        TUBtoBPRMapping.TryGetValue(tub.Format, out DXGI_FORMAT tubbprFormat);
                        bpr.Format = tubbprFormat;
                        sourceFormatIndex = (int)tub.Format;
                        destinationFormatIndex = (int)tubbprFormat;
                        if (tubbprFormat == DXGI_FORMAT.DXGI_FORMAT_UNKNOWN)
                            LogWarning($"Destination texture format is {tubbprFormat}! (Source is {tub.Format})");
                        break;
                    case (TextureBPR bpr, TexturePC tub):
                        BPRtoTUBMapping.TryGetValue(bpr.Format, out D3DFORMAT bprtubFormat);
                        tub.Format = bprtubFormat;
                        sourceFormatIndex = (int)bpr.Format;
                        destinationFormatIndex = (int)bprtubFormat;
                        if (bprtubFormat == D3DFORMAT.D3DFMT_UNKNOWN)
                            LogWarning($"Destination texture format is {bprtubFormat}! (Source is {bpr.Format})");
                        break;
                    case (TexturePS3 ps3, TextureBPR bpr):
                        PS3toBPRMapping.TryGetValue(ps3.Format, out DXGI_FORMAT ps3bprFormat);
                        bpr.Format = ps3bprFormat;
                        flipEndian = true;
                        sourceFormatIndex = (int)ps3.Format;
                        destinationFormatIndex = (int)ps3bprFormat;
                        if (ps3bprFormat == DXGI_FORMAT.DXGI_FORMAT_UNKNOWN)
                            LogWarning($"Destination texture format is {ps3bprFormat}! (Source is {ps3.Format})");
                        break;
                    case (TexturePS3 ps3, TexturePC tub):
                        PS3toTUBMapping.TryGetValue(ps3.Format, out D3DFORMAT ps3tubFormat);
                        tub.Format = ps3tubFormat;
                        flipEndian = true;
                        sourceFormatIndex = (int)ps3.Format;
                        destinationFormatIndex = (int)ps3tubFormat;
                        if (ps3tubFormat == D3DFORMAT.D3DFMT_UNKNOWN)
                            LogWarning($"Destination texture format is {ps3tubFormat}! (Source is {ps3.Format})");
                        break;
                    case (TextureX360 x360, TexturePC tub):
                        X360toTUBMapping.TryGetValue(x360.Format.DataFormat, out D3DFORMAT x360tubFormat);
                        tub.Format = x360tubFormat;
                        flipEndian = true;
                        sourceFormatIndex = (int)x360.Format.DataFormat;
                        destinationFormatIndex = (int)x360tubFormat;
                        if (x360tubFormat == D3DFORMAT.D3DFMT_UNKNOWN)
                            LogWarning($"Destination texture format is {x360tubFormat}! (Source is {x360.Format.DataFormat})");
                        break;
                    case (TextureX360 x360, TextureBPR bpr):
                        X360toBPRMapping.TryGetValue(x360.Format.DataFormat, out DXGI_FORMAT x360bprFormat);
                        bpr.Format = x360bprFormat;
                        flipEndian = true;
                        sourceFormatIndex = (int)x360.Format.DataFormat;
                        destinationFormatIndex = (int)x360bprFormat;
                        if (x360bprFormat == DXGI_FORMAT.DXGI_FORMAT_UNKNOWN)
                            LogWarning($"Destination texture format is {x360bprFormat}! (Source is {x360.Format.DataFormat})");
                        break;
                    case (TexturePC tub, TextureX360 x360):
                        TUBtoX360Mapping.TryGetValue(tub.Format, out GPUTEXTUREFORMAT tubx360Format);
                        x360.Format.DataFormat = tubx360Format;
                        flipEndian = true;
                        sourceFormatIndex = (int)tub.Format;
                        destinationFormatIndex = (int)tubx360Format;
                        if (tubx360Format == GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_1_REVERSE)
                            LogWarning($"Destination texture format is {tubx360Format}! (Source is {tub.Format})");
                        break;
                    case (TextureBPR bpr, TexturePS3 ps3):
                        BPRtoPS3Mapping.TryGetValue(bpr.Format, out CELL_GCM_COLOR_FORMAT bprps3format);
                        ps3.Format = bprps3format;
                        flipEndian = true;
                        sourceFormatIndex = (int)bpr.Format;
                        destinationFormatIndex = (int)bprps3format;
                        if (bprps3format == CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_INVALID)
                            LogWarning($"Destination texture format is {bprps3format}! (Source is {bpr.Format})");
                        break;
                    case (TexturePC tub, TexturePS3 ps3):
                        TUBtoPS3Mapping.TryGetValue(tub.Format, out CELL_GCM_COLOR_FORMAT tubps3Format);
                        ps3.Format = tubps3Format;
                        flipEndian = true;
                        sourceFormatIndex = (int)tub.Format;
                        destinationFormatIndex = (int)tubps3Format;
                        if (tubps3Format == CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_INVALID)
                            LogWarning($"Destination texture format is {tubps3Format}! (Source is {tub.Format})");
                        break;
                    case (TextureBPR bpr, TextureX360 x360):
                        BPRtoX360Mapping.TryGetValue(bpr.Format, out GPUTEXTUREFORMAT bprx360Format);
                        x360.Format.DataFormat = bprx360Format;
                        flipEndian = true;
                        sourceFormatIndex = (int)bpr.Format;
                        destinationFormatIndex = (int)bprx360Format;
                        if (bprx360Format == GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_1_REVERSE)
                            LogWarning($"Destination texture format is {bprx360Format}! (Source is {bpr.Format})");
                        break;
                    default:
                        throw new NotImplementedException($"Conversion technique {localSourceFormat} > {localDestinationFormat} is not yet implemented.");
                };

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

                    if (!TryConvertTexture(sourceTexture, destinationTexture, sourceBitmapData, destinationBitmapPath))
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

    private static void CopyProperties(TextureBase source, TextureBase destination)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (destination == null) throw new ArgumentNullException(nameof(destination));

        Type srcType = source.GetType();
        Type dstType = destination.GetType();

        Type typeToReflect = srcType == dstType
            ? srcType
            : typeof(TextureBase);

        IEnumerable<PropertyInfo> props = typeToReflect
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead
                     && p.CanWrite
                     && p.GetIndexParameters().Length == 0);

        foreach (PropertyInfo prop in props)
        {
            object? value = prop.GetValue(source);
            prop.SetValue(destination, value);
        }
    }

    private bool TryConvertTexture(TextureBase srcTexture, TextureBase destTexture, byte[] bitmap, string outPath)
    {
        switch (srcTexture, destTexture)
        {
            case (TexturePS3 ps3, TextureBPR bpr):
                if (ps3.Format == CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_A8R8G8B8)
                {
                    if (bpr.Format == DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM)
                    {
                        DDSTextureUtilities.A8R8G8B8toR8G8B8A8(bitmap, ps3.Width, ps3.Height, ps3.MipmapLevels);
                        break;
                    }

                    if (bpr.Format == DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM)
                    {
                        DDSTextureUtilities.A8R8G8B8toB8G8R8A8(bitmap, ps3.Width, ps3.Height, ps3.MipmapLevels);
                        break;
                    }
                }
                bitmap = Array.Empty<byte>();
                return false;
            case (TexturePS3 ps3, TexturePC tub):
                if (ps3.Format == CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_A8R8G8B8)
                {
                    if (tub.Format == D3DFORMAT.D3DFMT_A8R8G8B8)
                    {
                        break;
                    }

                    if (tub.Format == D3DFORMAT.D3DFMT_A8B8G8R8)
                    {
                        DDSTextureUtilities.A8R8G8B8toA8B8G8R8(bitmap, ps3.Width, ps3.Height, ps3.MipmapLevels);
                        break;
                    }
                }
                bitmap = Array.Empty<byte>();
                return false;
            case (TexturePS3 ps3, TextureX360 x360):
                if (ps3.Format == CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_A8R8G8B8
                && x360.Format.DataFormat == GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_8_8_8_8)
                {
                    break;
                }
                bitmap = Array.Empty<byte>();
                return false;
            case (TexturePC tub, TextureBPR bpr):
                if (tub.Format == D3DFORMAT.D3DFMT_A8R8G8B8
                && bpr.Format == DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM)
                    DDSTextureUtilities.A8R8G8B8toB8G8R8A8(bitmap, destTexture.Width, destTexture.Height, destTexture.MipmapLevels);
                if (tub.Format == D3DFORMAT.D3DFMT_A8B8G8R8
                && bpr.Format == DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM)
                    DDSTextureUtilities.A8B8G8R8toB8G8R8A8(bitmap, destTexture.Width, destTexture.Height, destTexture.MipmapLevels);
                break;
            case (TextureBPR bpr, TexturePS3 ps3):
                if (ps3.Format == CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_A8R8G8B8)
                {
                    if (bpr.Format == DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM)
                    {
                        DDSTextureUtilities.R8G8B8A8toA8R8G8B8(bitmap, bpr.Width, bpr.Height, bpr.MipmapLevels);
                        bitmap = PS3TextureUtilities.EncodePS3A8R8G8B8(bitmap, bpr.Width, bpr.Height, bpr.MipmapLevels);
                        break;
                    }

                    if (bpr.Format == DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM)
                    {
                        DDSTextureUtilities.B8G8R8A8toA8R8G8B8(bitmap, bpr.Width, bpr.Height, bpr.MipmapLevels);
                        bitmap = PS3TextureUtilities.EncodePS3A8R8G8B8(bitmap, bpr.Width, bpr.Height, bpr.MipmapLevels);
                        break;
                    }
                }
                bitmap = Array.Empty<byte>();
                return false;
            case (TexturePC tub, TexturePS3 ps3):
                if (ps3.Format == CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_A8R8G8B8)
                {
                    if (tub.Format == D3DFORMAT.D3DFMT_A8R8G8B8)
                    {
                        bitmap = PS3TextureUtilities.EncodePS3A8R8G8B8(bitmap, tub.Width, tub.Height, tub.MipmapLevels);
                        break;
                    }

                    if (tub.Format == D3DFORMAT.D3DFMT_A8B8G8R8)
                    {
                        DDSTextureUtilities.A8B8G8R8toA8R8G8B8(bitmap, tub.Width, tub.Height, tub.MipmapLevels);
                        bitmap = PS3TextureUtilities.EncodePS3A8R8G8B8(bitmap, tub.Width, tub.Height, tub.MipmapLevels);
                        break;
                    }
                }
                bitmap = Array.Empty<byte>();
                return false;
            case (TextureX360 x360, TexturePS3 ps3):
                if (ps3.Format == CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_A8R8G8B8
                && x360.Format.DataFormat == GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_8_8_8_8)
                {
                    bitmap = PS3TextureUtilities.EncodePS3A8R8G8B8(bitmap, x360.Width, x360.Height, x360.MipmapLevels);
                    break;
                }
                bitmap = Array.Empty<byte>();
                return false;
            default:
                bitmap = Array.Empty<byte>();
                return false;
        };
        File.WriteAllBytes(outPath, bitmap);
        return true;
    }

    private static readonly Dictionary<GPUTEXTUREFORMAT, D3DFORMAT> X360toTUBMapping = new()
    {
        { GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT1, D3DFORMAT.D3DFMT_DXT1 },
        { GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT2_3, D3DFORMAT.D3DFMT_DXT3 },
        { GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT4_5, D3DFORMAT.D3DFMT_DXT5 },
        { GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_8, D3DFORMAT.D3DFMT_A8 },
        { GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_16_16_16_16, D3DFORMAT.D3DFMT_A16B16G16R16 },
        { GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_8_8_8_8, D3DFORMAT.D3DFMT_A8R8G8B8 },
    };

    private static readonly Dictionary<GPUTEXTUREFORMAT, DXGI_FORMAT> X360toBPRMapping = new()
    {
        { GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT1, DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM },
        { GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT2_3, DXGI_FORMAT.DXGI_FORMAT_BC2_UNORM },
        { GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT4_5, DXGI_FORMAT.DXGI_FORMAT_BC3_UNORM },
        { GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_8, DXGI_FORMAT.DXGI_FORMAT_A8_UNORM },
        { GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_16_16_16_16, DXGI_FORMAT.DXGI_FORMAT_R16G16B16A16_UNORM },
        { GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_8_8_8_8, DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM },
    };

    private static readonly Dictionary<D3DFORMAT, DXGI_FORMAT> TUBtoBPRMapping = new()
    {
        { D3DFORMAT.D3DFMT_DXT1, DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM },
        { D3DFORMAT.D3DFMT_DXT3, DXGI_FORMAT.DXGI_FORMAT_BC2_UNORM },
        { D3DFORMAT.D3DFMT_DXT5, DXGI_FORMAT.DXGI_FORMAT_BC3_UNORM },
        { D3DFORMAT.D3DFMT_A8, DXGI_FORMAT.DXGI_FORMAT_A8_UNORM },
        { D3DFORMAT.D3DFMT_A8B8G8R8, DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM },
        { D3DFORMAT.D3DFMT_A8R8G8B8, DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM },
    };

    private static readonly Dictionary<DXGI_FORMAT, D3DFORMAT> BPRtoTUBMapping = new()
    {
        { DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM, D3DFORMAT.D3DFMT_DXT1 },
        { DXGI_FORMAT.DXGI_FORMAT_BC2_UNORM, D3DFORMAT.D3DFMT_DXT3 },
        { DXGI_FORMAT.DXGI_FORMAT_BC3_UNORM, D3DFORMAT.D3DFMT_DXT5 },
        { DXGI_FORMAT.DXGI_FORMAT_A8_UNORM, D3DFORMAT.D3DFMT_A8 },
        { DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM, D3DFORMAT.D3DFMT_A8B8G8R8 },
    };

    private static readonly Dictionary<CELL_GCM_COLOR_FORMAT, D3DFORMAT> PS3toTUBMapping = new()
    {
        { CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_COMPRESSED_DXT1, D3DFORMAT.D3DFMT_DXT1 },
        { CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_COMPRESSED_DXT23, D3DFORMAT.D3DFMT_DXT3 },
        { CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_COMPRESSED_DXT45, D3DFORMAT.D3DFMT_DXT5 },
        { CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_B8, D3DFORMAT.D3DFMT_A8 },
        { CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_A8R8G8B8, D3DFORMAT.D3DFMT_A8R8G8B8 },
    };

    private static readonly Dictionary<CELL_GCM_COLOR_FORMAT, GPUTEXTUREFORMAT> PS3toX360Mapping = new()
    {
        { CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_COMPRESSED_DXT1, GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT1 },
        { CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_COMPRESSED_DXT23, GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT2_3 },
        { CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_COMPRESSED_DXT45, GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT4_5 },
        { CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_B8, GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_8_B },
        { CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_A8R8G8B8, GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_8_8_8_8 },
    };

    private static readonly Dictionary<D3DFORMAT, GPUTEXTUREFORMAT> TUBtoX360Mapping = new()
    {
        { D3DFORMAT.D3DFMT_DXT1, GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT1 },
        { D3DFORMAT.D3DFMT_DXT3, GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT2_3 },
        { D3DFORMAT.D3DFMT_DXT5, GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT4_5 },
    };

    private static readonly Dictionary<GPUTEXTUREFORMAT, CELL_GCM_COLOR_FORMAT> X360toPS3Mapping = new()
    {
        { GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT1, CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_COMPRESSED_DXT1 },
        { GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT2_3, CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_COMPRESSED_DXT23 },
        { GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT4_5, CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_COMPRESSED_DXT45 },
        { GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_8_B, CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_B8 },
        { GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_8_8_8_8, CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_A8R8G8B8 },
    };

    private static readonly Dictionary<CELL_GCM_COLOR_FORMAT, DXGI_FORMAT> PS3toBPRMapping = new()
    {
        { CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_COMPRESSED_DXT1, DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM },
        { CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_COMPRESSED_DXT23, DXGI_FORMAT.DXGI_FORMAT_BC2_UNORM },
        { CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_COMPRESSED_DXT45, DXGI_FORMAT.DXGI_FORMAT_BC3_UNORM },
        { CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_B8, DXGI_FORMAT.DXGI_FORMAT_A8_UNORM },
        { CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_A8R8G8B8, DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM }
    };

    private static readonly Dictionary<DXGI_FORMAT, CELL_GCM_COLOR_FORMAT> BPRtoPS3Mapping = new()
    {
        { DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM, CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_COMPRESSED_DXT1 },
        { DXGI_FORMAT.DXGI_FORMAT_BC2_UNORM, CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_COMPRESSED_DXT23 },
        { DXGI_FORMAT.DXGI_FORMAT_BC3_UNORM, CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_COMPRESSED_DXT45 },
        { DXGI_FORMAT.DXGI_FORMAT_A8_UNORM, CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_B8 },
        { DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM, CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_A8R8G8B8 },
        { DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM, CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_A8R8G8B8 },
    };

    private static readonly Dictionary<D3DFORMAT, CELL_GCM_COLOR_FORMAT> TUBtoPS3Mapping = new()
    {
        { D3DFORMAT.D3DFMT_DXT1, CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_COMPRESSED_DXT1 },
        { D3DFORMAT.D3DFMT_DXT3, CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_COMPRESSED_DXT23 },
        { D3DFORMAT.D3DFMT_DXT5, CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_COMPRESSED_DXT45 },
        { D3DFORMAT.D3DFMT_A8, CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_B8 },
        { D3DFORMAT.D3DFMT_A8R8G8B8, CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_A8R8G8B8 },
        { D3DFORMAT.D3DFMT_A8B8G8R8, CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_A8R8G8B8 },
    };

    private static readonly Dictionary<DXGI_FORMAT, GPUTEXTUREFORMAT> BPRtoX360Mapping = new()
    {
        { DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM, GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT1 },
        { DXGI_FORMAT.DXGI_FORMAT_BC2_UNORM, GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT2_3 },
        { DXGI_FORMAT.DXGI_FORMAT_BC3_UNORM, GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT4_5 },
    };

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
