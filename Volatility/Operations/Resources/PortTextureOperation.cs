using System.Reflection;

using Volatility.Resources;
using Volatility.Utilities;

using static Volatility.Utilities.ResourceIDUtilities;

namespace Volatility.Operations.Resources;

internal class PortTextureOperation
{
    public async Task ExecuteAsync(IEnumerable<string> sourceFiles, string sourceFormat, string sourcePath, string destinationFormat, string? destinationPath, bool verbose, bool useGtf)
    {
        string resolvedDestinationPath = string.IsNullOrEmpty(destinationPath) ? sourcePath : destinationPath;

        List<Task> tasks = new List<Task>();

        foreach (string sourceFile in sourceFiles)
        {
            tasks.Add(Task.Run(async () =>
            {
                TextureBase? sourceTexture = ConstructHeader(sourceFile, sourceFormat, verbose);
                TextureBase? destinationTexture = ConstructHeader(resolvedDestinationPath, destinationFormat, verbose);

                if (sourceTexture == null || destinationTexture == null)
                {
                    throw new InvalidOperationException("Failed to initialize texture header. Ensure the platform matches the file format and that the path is correct.");
                }

                string localSourceFormat = BPRx64Hack(sourceTexture, sourceFormat);
                string localDestinationFormat = BPRx64Hack(destinationTexture, destinationFormat);

                sourceTexture.PullAll();

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
                            Console.WriteLine($"WARNING: Destination texture format is {ps3x360Format}! (Source is {ps3.Format})");
                        break;
                    case (TextureX360 x360, TexturePS3 ps3):
                        X360toPS3Mapping.TryGetValue(x360.Format.DataFormat, out CELL_GCM_COLOR_FORMAT x360ps3Format);
                        ps3.Format = x360ps3Format;
                        flipEndian = false;
                        sourceFormatIndex = (int)x360.Format.DataFormat;
                        destinationFormatIndex = (int)x360ps3Format;
                        if (x360ps3Format == CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_INVALID)
                            Console.WriteLine($"WARNING: Destination texture format is {x360ps3Format}! (Source is {x360.Format.DataFormat})");
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
                            Console.WriteLine($"WARNING: Destination texture format is {tubbprFormat}! (Source is {tub.Format})");
                        break;
                    case (TextureBPR bpr, TexturePC tub):
                        BPRtoTUBMapping.TryGetValue(bpr.Format, out D3DFORMAT bprtubFormat);
                        tub.Format = bprtubFormat;
                        sourceFormatIndex = (int)bpr.Format;
                        destinationFormatIndex = (int)bprtubFormat;
                        if (bprtubFormat == D3DFORMAT.D3DFMT_UNKNOWN)
                            Console.WriteLine($"WARNING: Destination texture format is {bprtubFormat}! (Source is {bpr.Format})");
                        break;
                    case (TexturePS3 ps3, TextureBPR bpr):
                        PS3toBPRMapping.TryGetValue(ps3.Format, out DXGI_FORMAT ps3bprFormat);
                        bpr.Format = ps3bprFormat;
                        flipEndian = true;
                        sourceFormatIndex = (int)ps3.Format;
                        destinationFormatIndex = (int)ps3bprFormat;
                        if (ps3bprFormat == DXGI_FORMAT.DXGI_FORMAT_UNKNOWN)
                            Console.WriteLine($"WARNING: Destination texture format is {ps3bprFormat}! (Source is {ps3.Format})");
                        break;
                    case (TexturePS3 ps3, TexturePC tub):
                        PS3toTUBMapping.TryGetValue(ps3.Format, out D3DFORMAT ps3tubFormat);
                        tub.Format = ps3tubFormat;
                        flipEndian = true;
                        sourceFormatIndex = (int)ps3.Format;
                        destinationFormatIndex = (int)ps3tubFormat;
                        if (ps3tubFormat == D3DFORMAT.D3DFMT_UNKNOWN)
                            Console.WriteLine($"WARNING: Destination texture format is {ps3tubFormat}! (Source is {ps3.Format})");
                        break;
                    case (TextureX360 x360, TexturePC tub):
                        X360toTUBMapping.TryGetValue(x360.Format.DataFormat, out D3DFORMAT x360tubFormat);
                        tub.Format = x360tubFormat;
                        flipEndian = true;
                        sourceFormatIndex = (int)x360.Format.DataFormat;
                        destinationFormatIndex = (int)x360tubFormat;
                        if (x360tubFormat == D3DFORMAT.D3DFMT_UNKNOWN)
                            Console.WriteLine($"WARNING: Destination texture format is {x360tubFormat}! (Source is {x360.Format.DataFormat})");
                        break;
                    case (TextureX360 x360, TextureBPR bpr):
                        X360toBPRMapping.TryGetValue(x360.Format.DataFormat, out DXGI_FORMAT x360bprFormat);
                        bpr.Format = x360bprFormat;
                        flipEndian = true;
                        sourceFormatIndex = (int)x360.Format.DataFormat;
                        destinationFormatIndex = (int)x360bprFormat;
                        if (x360bprFormat == DXGI_FORMAT.DXGI_FORMAT_UNKNOWN)
                            Console.WriteLine($"WARNING: Destination texture format is {x360bprFormat}! (Source is {x360.Format.DataFormat})");
                        break;
                    case (TexturePC tub, TextureX360 x360):
                        TUBtoX360Mapping.TryGetValue(tub.Format, out GPUTEXTUREFORMAT tubx360Format);
                        x360.Format.DataFormat = tubx360Format;
                        flipEndian = true;
                        sourceFormatIndex = (int)tub.Format;
                        destinationFormatIndex = (int)tubx360Format;
                        if (tubx360Format == GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_1_REVERSE)
                            Console.WriteLine($"WARNING: Destination texture format is {tubx360Format}! (Source is {tub.Format})");
                        break;
                    case (TextureBPR bpr, TexturePS3 ps3):
                        BPRtoPS3Mapping.TryGetValue(bpr.Format, out CELL_GCM_COLOR_FORMAT bprps3format);
                        ps3.Format = bprps3format;
                        flipEndian = true;
                        sourceFormatIndex = (int)bpr.Format;
                        destinationFormatIndex = (int)bprps3format;
                        if (bprps3format == CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_INVALID)
                            Console.WriteLine($"WARNING: Destination texture format is {bprps3format}! (Source is {bpr.Format})");
                        break;
                    case (TextureBPR bpr, TextureX360 x360):
                        BPRtoX360Mapping.TryGetValue(bpr.Format, out GPUTEXTUREFORMAT bprx360Format);
                        x360.Format.DataFormat = bprx360Format;
                        flipEndian = true;
                        sourceFormatIndex = (int)bpr.Format;
                        destinationFormatIndex = (int)bprx360Format;
                        if (bprx360Format == GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_1_REVERSE)
                            Console.WriteLine($"WARNING: Destination texture format is {bprx360Format}! (Source is {bpr.Format})");
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
                else if (new DirectoryInfo(resolvedDestinationPath).Exists)
                {
                    outPath = resolvedDestinationPath + Path.DirectorySeparatorChar + outResourceFilename;
                }

                string secondaryExtension = sourceTexture.Unpacker switch
                {
                    Unpacker.Bnd2Manager => "_2.bin",
                    Unpacker.DGI => "_texture.dat",
                    Unpacker.YAP => "_secondary.dat",
                    Unpacker.Raw => "_texture.dat",
                    Unpacker.Volatility => throw new NotImplementedException(),
                    _ => throw new NotImplementedException(),
                };

                string primaryExtension = sourceTexture.Unpacker switch
                {
                    Unpacker.Bnd2Manager => "_1.bin",
                    Unpacker.DGI => ".dat",
                    Unpacker.YAP => "_primary.dat",
                    Unpacker.Raw => ".dat",
                    Unpacker.Volatility => throw new NotImplementedException(),
                    _ => throw new NotImplementedException(),
                };

                string sourceBitmapPath = $"{Path.GetDirectoryName(sourceFile)}{Path.DirectorySeparatorChar}{Path.GetFileName(sourceFile).Split(primaryExtension)[0]}{secondaryExtension}";

                if (!Path.Exists(sourceBitmapPath))
                {
                    Console.WriteLine($"Failed to find associated bitmap data for {Path.GetFileNameWithoutExtension(sourceFile)} at path {sourceBitmapPath}!");
                }

                string destinationBitmapPath = $"{Path.GetDirectoryName(outPath)}{Path.DirectorySeparatorChar}{Path.GetFileName(sourceFile).Split(primaryExtension)[0]}{secondaryExtension}";

                if (Path.Exists(destinationBitmapPath))
                {
                    if (verbose) Console.WriteLine($"Found existing bitmap data at {destinationBitmapPath}, overwriting...");
                }

                try
                {
                    if (useGtf && sourceTexture.GetResourcePlatform() == Platform.PS3)
                    {
                        PS3TextureUtilities.PS3GTFToDDS(sourcePath, sourceBitmapPath, destinationBitmapPath, verbose);
                    }

                    if (destinationTexture is TextureX360 destX && sourceTexture.GetResourcePlatform() != Platform.X360)
                    {
                        destX.Format.MaxMipLevel = destX.Format.MinMipLevel;
                    }
                    if (sourceTexture is TextureX360 sourceX)
                    {
                        if (sourceX.Format.Tiled && !string.IsNullOrEmpty(sourceBitmapPath))
                        {
                            if (verbose) Console.WriteLine($"Detiling X360 bitmap data for {Path.GetDirectoryName(outPath)}{Path.DirectorySeparatorChar}{Path.GetFileNameWithoutExtension(outPath)}_texture.dat...");
                            X360TextureUtilities.WriteUntiled360TextureFile(sourceX, sourceBitmapPath, destinationBitmapPath);
                        }
                    }
                    else
                    {
                        if (!TryConvertTexture(sourceTexture, destinationTexture, sourceBitmapPath, destinationBitmapPath))
                        {
                            if (verbose) Console.WriteLine($"Copying associated bitmap data for {Path.GetDirectoryName(outPath)}{Path.DirectorySeparatorChar}{Path.GetFileNameWithoutExtension(outPath)}_texture.dat...");
                            File.Copy(sourceBitmapPath, destinationBitmapPath, true);
                        }
                        else
                        {
                            if (verbose) Console.WriteLine($"Converting associated bitmap data for {Path.GetDirectoryName(outPath)}{Path.DirectorySeparatorChar}{Path.GetFileNameWithoutExtension(outPath)}_texture.dat...");
                        }
                    }
                    if (verbose) Console.WriteLine($"Wrote texture bitmap data to {destinationFormat} destination directory.");

                    if (destinationTexture is TextureBPR destBprTexture && File.Exists(destinationBitmapPath))
                    {
                        destBprTexture.PlacedDataSize = (uint)new FileInfo(destinationBitmapPath).Length;
                        if (verbose) Console.WriteLine($"BPR PlacedDataSize set to {destBprTexture.PlacedDataSize} (file: {destinationBitmapPath}).");
                    }

                    using FileStream fs = new(outPath, FileMode.Create, FileAccess.Write);
                    using (EndianAwareBinaryWriter writer = new(fs, destinationTexture.GetResourceEndian()))
                    {
                        try
                        {
                            if (verbose) Console.WriteLine($"Writing converted {destinationFormat} texture property data to destination file {Path.GetFileName(outPath)}...");
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
                    Console.WriteLine($"Error trying to copy bitmap data for {Path.GetFileNameWithoutExtension(sourceFile)}: {ex.Message}");
                }

                Console.WriteLine($"Successfully ported {localSourceFormat} formatted {Path.GetFileNameWithoutExtension(sourceFile)}to {localDestinationFormat} as {Path.GetFileNameWithoutExtension(outPath)}.");
            }));
        }

        await Task.WhenAll(tasks);
    }

    private string BPRx64Hack(TextureBase header, string format)
    {
        if (header.GetResourcePlatform() == Platform.BPR && format.EndsWith("X64", StringComparison.Ordinal))
        {
            header.SetResourceArch(Arch.x64);
            return "BPR";
        }
        return format;
    }

    private static TextureBase? ConstructHeader(string path, string format, bool verbose)
    {
        if (verbose) Console.WriteLine($"Constructing {format} texture property data...");
        return format switch
        {
            "BPR" => new TextureBPR(path),
            "BPRX64" => new TextureBPR(path),
            "TUB" => new TexturePC(path),
            "X360" => new TextureX360(path),
            "PS3" => new TexturePS3(path),
            _ => throw new InvalidPlatformException(),
        };
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

    private bool TryConvertTexture(TextureBase srcTexture, TextureBase destTexture, string inPath, string outPath)
    {
        byte[] bitmap = File.ReadAllBytes(inPath);
        switch (srcTexture, destTexture)
        {
            case (TexturePS3 ps3, TextureBPR bpr):
                if (ps3.Format == CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_A8R8G8B8
                && bpr.Format == DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM)
                    DDSTextureUtilities.A8R8G8B8toB8G8R8A8(bitmap, destTexture.Width, destTexture.Height, destTexture.MipmapLevels);
                break;
            case (TexturePC tub, TextureBPR bpr):
                if (tub.Format == D3DFORMAT.D3DFMT_A8R8G8B8
                && bpr.Format == DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM)
                    DDSTextureUtilities.A8R8G8B8toB8G8R8A8(bitmap, destTexture.Width, destTexture.Height, destTexture.MipmapLevels);
                if (tub.Format == D3DFORMAT.D3DFMT_A8B8G8R8
                && bpr.Format == DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM)
                    DDSTextureUtilities.A8B8G8R8toB8G8R8A8(bitmap, destTexture.Width, destTexture.Height, destTexture.MipmapLevels);
                break;
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
    };

    private static readonly Dictionary<DXGI_FORMAT, GPUTEXTUREFORMAT> BPRtoX360Mapping = new()
    {
        { DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM, GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT1 },
        { DXGI_FORMAT.DXGI_FORMAT_BC2_UNORM, GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT2_3 },
        { DXGI_FORMAT.DXGI_FORMAT_BC3_UNORM, GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT4_5 },
    };
}
