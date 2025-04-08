using Volatility.Resources;
using Volatility.Utilities;

using static Volatility.Utilities.ResourceIDUtilities;

namespace Volatility.CLI.Commands;

internal class PortTextureCommand : ICommand
{
    public static string CommandToken => "PortTexture";
    public static string CommandDescription => "Ports texture data from a given source format to the specified destination format.";
    public static string CommandParameters => "[--verbose] [--usegtf] --informat=<tub,bpr[x64],x360,ps3> --inpath=<file/folder path> --outformat=<tub,bpr[x64],x360,ps3> [--outpath=<file/folder path>]";

    public string? SourceFormat { get; set; }
    public string? SourcePath { get; set; }
    public string? DestinationFormat { get; set; }
    public string? DestinationPath { get; set; }
    public bool Verbose { get; set; }
    public bool UseGTF { get; set; }

    public async Task Execute()
    {
        if (string.IsNullOrEmpty(SourcePath))
        {
            Console.WriteLine("Error: No source path specified! (--inpath)");
            return;
        }

        var sourceFiles = ICommand.GetFilePathsInDirectory(SourcePath, ICommand.TargetFileType.Header);
        List<Task> tasks = new List<Task>();

        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine($"Starting {sourceFiles.Length} PortTexture tasks...");
        Console.ResetColor();

        foreach (string sourceFile in sourceFiles)
        {
            tasks.Add(Task.Run(async () =>
            {
                TextureHeaderBase? SourceTexture = ConstructHeader(sourceFile, SourceFormat, Verbose);
                TextureHeaderBase? DestinationTexture = ConstructHeader(DestinationPath, DestinationFormat, Verbose);

                if (SourceTexture == null || DestinationTexture == null)
                {
                    throw new InvalidOperationException("Failed to initialize texture header. Ensure the platform matches the file format and that the path is correct.");
                }

                // TODO: Cleanup!!
                SourceFormat = BPRx64Hack(SourceTexture, SourceFormat);
                DestinationFormat = BPRx64Hack(DestinationTexture, DestinationFormat);

                SourceTexture.PullAll();

                CopyBaseClassProperties(SourceTexture, DestinationTexture);

                // Manual header format conversion
                var technique = $"{SourceFormat}>>{DestinationFormat}";
                bool flipEndian = false;
                int sourceFormatIndex = 0;
                int destinationFormatIndex = 0;
                switch (technique)
                {
                    // ==== Console <> Console (no endian flip)

                    case "PS3>>X360":
                        PS3toX360Mapping.TryGetValue((SourceTexture as TextureHeaderPS3).Format, out GPUTEXTUREFORMAT ps3x360Format);
                        if (DestinationTexture is TextureHeaderX360 x)
                        {
                            x.Format.DataFormat = ps3x360Format;
                            x.Format.Endian = GPUENDIAN.GPUENDIAN_NONE; // This may need to be the default value for new 360 textures
                        }
                        flipEndian = false;
                        sourceFormatIndex = (int)(SourceTexture as TextureHeaderPS3).Format;
                        destinationFormatIndex = (int)ps3x360Format;
                        if (ps3x360Format == GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_1_REVERSE)
                            Console.WriteLine($"WARNING: Destination texture format is {ps3x360Format}! (Source is {(SourceTexture as TextureHeaderPS3).Format})");
                        break;
                    case "X360>>PS3":
                        X360toPS3Mapping.TryGetValue((SourceTexture as TextureHeaderX360).Format.DataFormat, out CELL_GCM_COLOR_FORMAT x360ps3Format);
                        (DestinationTexture as TextureHeaderPS3).Format = x360ps3Format;
                        flipEndian = false;
                        sourceFormatIndex = (int)(SourceTexture as TextureHeaderX360).Format.DataFormat;
                        destinationFormatIndex = (int)x360ps3Format;
                        if (x360ps3Format == CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_INVALID)
                            Console.WriteLine($"WARNING: Destination texture format is {x360ps3Format}! (Source is {(SourceTexture as TextureHeaderX360).Format.DataFormat})");
                        break;

                    // ==== PC/BPR <> PC/BPR (no endian flip)

                    case "BPR>>BPR":
                        // I don't know why this is required as base class format should be copied anyway.
                        (DestinationTexture as TextureHeaderBPR).Format = (SourceTexture as TextureHeaderBPR).Format;
                        sourceFormatIndex = (int)(SourceTexture as TextureHeaderBPR).Format;
                        destinationFormatIndex = sourceFormatIndex;
                        break;
                    case "TUB>>BPR":
                        TUBtoBPRMapping.TryGetValue((SourceTexture as TextureHeaderPC).Format, out DXGI_FORMAT tubbprFormat);
                        (DestinationTexture as TextureHeaderBPR).Format = tubbprFormat;
                        sourceFormatIndex = (int)(SourceTexture as TextureHeaderPC).Format;
                        destinationFormatIndex = (int)tubbprFormat;
                        if (tubbprFormat == DXGI_FORMAT.DXGI_FORMAT_UNKNOWN) 
                            Console.WriteLine($"WARNING: Destination texture format is {tubbprFormat}! (Source is {(SourceTexture as TextureHeaderPC).Format})");
                        break;
                    case "BPR>>TUB":
                        BPRtoTUBMapping.TryGetValue((SourceTexture as TextureHeaderBPR).Format, out D3DFORMAT bprtubFormat);
                        (DestinationTexture as TextureHeaderPC).Format = bprtubFormat;
                        sourceFormatIndex = (int)(SourceTexture as TextureHeaderBPR).Format;
                        destinationFormatIndex = (int)bprtubFormat;
                        if (bprtubFormat == D3DFORMAT.D3DFMT_UNKNOWN)
                            Console.WriteLine($"WARNING: Destination texture format is {bprtubFormat}! (Source is {(SourceTexture as TextureHeaderBPR).Format})");
                        break;

                    // ==== Console <> PC/BPR (endian flip)

                    // = PS3 Source
                    case "PS3>>BPR":
                        PS3toBPRMapping.TryGetValue((SourceTexture as TextureHeaderPS3).Format, out DXGI_FORMAT ps3bprFormat);
                        (DestinationTexture as TextureHeaderBPR).Format = ps3bprFormat;
                        flipEndian = true;
                        sourceFormatIndex = (int)(SourceTexture as TextureHeaderPS3).Format;
                        destinationFormatIndex = (int)ps3bprFormat;
                        if (ps3bprFormat == DXGI_FORMAT.DXGI_FORMAT_UNKNOWN)
                            Console.WriteLine($"WARNING: Destination texture format is {ps3bprFormat}! (Source is {(SourceTexture as TextureHeaderPS3).Format})");
                        break;
                    case "PS3>>TUB":
                        PS3toTUBMapping.TryGetValue((SourceTexture as TextureHeaderPS3).Format, out D3DFORMAT ps3tubFormat);
                        (DestinationTexture as TextureHeaderPC).Format = ps3tubFormat;
                        flipEndian = true;
                        sourceFormatIndex = (int)(SourceTexture as TextureHeaderPS3).Format;
                        destinationFormatIndex = (int)ps3tubFormat;
                        if (ps3tubFormat == D3DFORMAT.D3DFMT_UNKNOWN)
                            Console.WriteLine($"WARNING: Destination texture format is {ps3tubFormat}! (Source is {(SourceTexture as TextureHeaderPS3).Format})");
                        break;

                    // = X360 Source
                    case "X360>>TUB":
                        X360toTUBMapping.TryGetValue((SourceTexture as TextureHeaderX360).Format.DataFormat, out D3DFORMAT x360tubFormat);
                        (DestinationTexture as TextureHeaderPC).Format = x360tubFormat;
                        flipEndian = true;
                        sourceFormatIndex = (int)(SourceTexture as TextureHeaderX360).Format.DataFormat;
                        destinationFormatIndex = (int)x360tubFormat;
                        if (x360tubFormat == D3DFORMAT.D3DFMT_UNKNOWN)
                            Console.WriteLine($"WARNING: Destination texture format is {x360tubFormat}! (Source is {(SourceTexture as TextureHeaderX360).Format.DataFormat})");
                        break;
                    case "X360>>BPR":
                        X360toBPRMapping.TryGetValue((SourceTexture as TextureHeaderX360).Format.DataFormat, out DXGI_FORMAT x360bprFormat);
                        (DestinationTexture as TextureHeaderBPR).Format = x360bprFormat;
                        flipEndian = true;
                        sourceFormatIndex = (int)(SourceTexture as TextureHeaderX360).Format.DataFormat;
                        destinationFormatIndex = (int)x360bprFormat;
                        if (x360bprFormat == DXGI_FORMAT.DXGI_FORMAT_UNKNOWN)
                            Console.WriteLine($"WARNING: Destination texture format is {x360bprFormat}! (Source is {(SourceTexture as TextureHeaderX360).Format.DataFormat})");
                        break;

                    // = TUB Source
                    case "TUB>>X360":
                        TUBtoX360Mapping.TryGetValue((SourceTexture as TextureHeaderPC).Format, out GPUTEXTUREFORMAT tubx360Format);
                        (DestinationTexture as TextureHeaderX360).Format.DataFormat = tubx360Format;
                        flipEndian = true;
                        sourceFormatIndex = (int)(SourceTexture as TextureHeaderPC).Format;
                        destinationFormatIndex = (int)tubx360Format;
                        if (tubx360Format == GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_1_REVERSE)
                            Console.WriteLine($"WARNING: Destination texture format is {tubx360Format}! (Source is {(SourceTexture as TextureHeaderPC).Format})");
                        break;
                    
                    // = BPR Source
                    case "BPR>>PS3":
                        BPRtoPS3Mapping.TryGetValue((SourceTexture as TextureHeaderBPR).Format, out CELL_GCM_COLOR_FORMAT bprps3format);
                        (DestinationTexture as TextureHeaderPS3).Format = bprps3format;
                        flipEndian = true;
                        sourceFormatIndex = (int)(SourceTexture as TextureHeaderBPR).Format;
                        destinationFormatIndex = (int)bprps3format;
                        if (bprps3format == CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_INVALID)
                            Console.WriteLine($"WARNING: Destination texture format is {bprps3format}! (Source is {(SourceTexture as TextureHeaderBPR).Format})");
                        break;
                    case "BPR>>X360":
                        BPRtoX360Mapping.TryGetValue((SourceTexture as TextureHeaderBPR).Format, out GPUTEXTUREFORMAT bprx360Format);
                        (DestinationTexture as TextureHeaderX360).Format.DataFormat = bprx360Format;
                        flipEndian = true;
                        sourceFormatIndex = (int)(SourceTexture as TextureHeaderBPR).Format;
                        destinationFormatIndex = (int)bprx360Format;
                        if (bprx360Format == GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_1_REVERSE)
                            Console.WriteLine($"WARNING: Destination texture format is {bprx360Format}! (Source is {(SourceTexture as TextureHeaderBPR).Format})");
                        break;

                    default:
                        throw new NotImplementedException($"Conversion technique {technique} is not yet implemented.");
                };

                // Finalize Destination
                DestinationTexture.PushAll();

                string outPath = @"";

                string outResourceFilename = (flipEndian && SourceTexture.Unpacker != Unpacker.YAP) 
                    ? FlipPathResourceIDEndian(Path.GetFileName(sourceFile)) 
                    : Path.GetFileName(sourceFile);

                if (DestinationPath == sourceFile)
                {
                    outPath = $"{Path.GetDirectoryName(DestinationPath)}{Path.DirectorySeparatorChar}{outResourceFilename}";
                }
                // If we're outputting to a directory
                else if (new DirectoryInfo(DestinationPath).Exists)
                {
                    outPath = DestinationPath + Path.DirectorySeparatorChar + outResourceFilename;
                }

                // TODO: Resource-defined Secondary path support
                string secondaryExtension = SourceTexture.Unpacker switch
                {
                    Unpacker.Bnd2Manager => "_2.bin",
                    Unpacker.DGI => "_texture.dat",
                    Unpacker.YAP => "_secondary.dat",
                    Unpacker.Raw => "_texture.dat", // Fallback for now
                    Unpacker.Volatility => throw new NotImplementedException(),
                    _ => throw new NotImplementedException(),
                };
                
                string primaryExtension = SourceTexture.Unpacker switch
                {
                    Unpacker.Bnd2Manager => "_1.bin",
                    Unpacker.DGI => ".dat",
                    Unpacker.YAP => "_primary.dat",
                    Unpacker.Raw => ".dat", // Fallback for now
                    Unpacker.Volatility => throw new NotImplementedException(),
                    _ => throw new NotImplementedException(),
                };

                string sourceBitmapPath = $"{Path.GetDirectoryName(sourceFile)}{Path.DirectorySeparatorChar}{Path.GetFileName(sourceFile).Split(primaryExtension)[0]}{secondaryExtension}";

                if (!Path.Exists(sourceBitmapPath))
                {
                    Console.WriteLine($"Failed to find associated texture data for {Path.GetFileNameWithoutExtension(sourceFile)} at path {sourceBitmapPath}!");
                }

                string destinationBitmapPath = $"{Path.GetDirectoryName(outPath)}{Path.DirectorySeparatorChar}{Path.GetFileName(sourceFile).Split(primaryExtension)[0]}{secondaryExtension}";

                if (Path.Exists(destinationBitmapPath))
                {
                    if (Verbose) Console.WriteLine($"Found existing texture data at {destinationBitmapPath}, overwriting...");
                }

                try
                {
                    // Currently requires an external tool. Every texture I've encountered on PS3 is
                    // already raw DDS anyway, so there's not really any reason to do this as far as I see.

                    if (UseGTF && SourceTexture.GetResourcePlatform() == Platform.PS3)
                    {
                        PS3TextureUtilities.PS3GTFToDDS(SourcePath, sourceBitmapPath, destinationBitmapPath, Verbose);
                    }

                    if (DestinationTexture is TextureHeaderX360 destX && SourceTexture.GetResourcePlatform() != Platform.X360)
                    {
                        destX.Format.MaxMipLevel = destX.Format.MinMipLevel;

                        //if (DestinationTexture.MipmapLevels > 0)
                        //{
                        //    // - Repack Mipmaps (WIP!)
                        //    try
                        //    {
                        //        if (Verbose) Console.WriteLine($"Converting mipmap data to X360 format for {Path.GetDirectoryName(outPath)}{Path.DirectorySeparatorChar}{Path.GetFileNameWithoutExtension(outPath)}_texture.dat...");
                        //        X360TextureUtilities.ConvertMipmapsToX360(destX, destX.Format.DataFormat, sourceBitmapPath, destinationBitmapPath);
                        //    }
                        //    catch (Exception e)
                        //    {
                        //        Console.WriteLine($"Error converting mipmap data to X360 format for {Path.GetFileNameWithoutExtension(sourceFile)}: {e.Message}");
                        //    }
                        //}
                    }
                    if (SourceTexture is TextureHeaderX360 sourceX)
                    {
                        if (sourceX.Format.Tiled && !string.IsNullOrEmpty(sourceBitmapPath))
                        {
                            if (Verbose) Console.WriteLine($"Detiling X360 bitmap data for {Path.GetDirectoryName(outPath)}{Path.DirectorySeparatorChar}{Path.GetFileNameWithoutExtension(outPath)}_texture.dat...");
                            X360TextureUtilities.WriteUntiled360TextureFile(sourceX, sourceBitmapPath, destinationBitmapPath);
                        }
                    }
                    else
                    {

                        if (!TryConvertTexture(DestinationTexture.Width,
                                          DestinationTexture.Height,
                                          DestinationTexture.MipmapLevels,
                                          technique,
                                          sourceFormatIndex,
                                          destinationFormatIndex,
                                          sourceBitmapPath,
                                          destinationBitmapPath))
                        {
                            if (Verbose) Console.WriteLine($"Copying associated bitmap data for {Path.GetDirectoryName(outPath)}{Path.DirectorySeparatorChar}{Path.GetFileNameWithoutExtension(outPath)}_texture.dat...");
                            File.Copy(sourceBitmapPath, destinationBitmapPath, true);
                        }
                        else
                        {
                            if (Verbose) Console.WriteLine($"Converting associated bitmap data for {Path.GetDirectoryName(outPath)}{Path.DirectorySeparatorChar}{Path.GetFileNameWithoutExtension(outPath)}_texture.dat...");
                        }
                    }
                    if (Verbose) Console.WriteLine($"Wrote texture bitmap data to {DestinationFormat} destination directory.");

                    // Set ContentsSize for BPR textures if applicable.
                    if (DestinationTexture is TextureHeaderBPR destBPRTexture && File.Exists(destinationBitmapPath))
                    {
                        destBPRTexture.ContentsSize = (uint)new FileInfo(destinationBitmapPath).Length;
                        if (Verbose) Console.WriteLine($"BPR ContentsSize set to {destBPRTexture.ContentsSize} (file: {destinationBitmapPath}).");
                    }

                    // Write header data (now after bitmap data to ensure any final edits are included)
                    using FileStream fs = new(outPath, FileMode.Create, FileAccess.Write);
                    using (EndianAwareBinaryWriter writer = new(fs, DestinationTexture.GetResourceEndian()))
                    {
                        try
                        {
                            if (Verbose) Console.WriteLine($"Writing converted {DestinationFormat} texture property data to destination file {Path.GetFileName(outPath)}...");
                            DestinationTexture.WriteToStream(writer);
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

                Console.WriteLine($"Successfully ported {SourceFormat} formatted {Path.GetFileNameWithoutExtension(sourceFile)} to {DestinationFormat} as {Path.GetFileNameWithoutExtension(outPath)}.");
            }));
        }

        await Task.WhenAll(tasks);
    }

    public void SetArgs(Dictionary<string, object> args)
    {
        SourceFormat = (args.TryGetValue("informat", out object? informat) ? informat as string
                : args.TryGetValue("if", out object? iff) ? iff as string : "auto").ToUpper();

        SourcePath = args.TryGetValue("inpath", out object? inpath) ? inpath as string
                : args.TryGetValue("ip", out object? ipp) ? ipp as string : "";

        DestinationFormat = (args.TryGetValue("outformat", out object? outformat) ? outformat as string
                : args.TryGetValue("of", out object? off) ? off as string : "auto").ToUpper();

        DestinationPath = args.TryGetValue("outpath", out object? outpath) ? inpath as string
                : args.TryGetValue("op", out object? opp) ? opp as string : SourcePath;

        Verbose = args.TryGetValue("verbose", out var verbose) && (bool)verbose;
        UseGTF = args.TryGetValue("usegtf", out var usegtf) && (bool)usegtf;
    }

    public string BPRx64Hack(TextureHeaderBase header, string format)
    {
        if (header.GetResourcePlatform() == Platform.BPR && format.EndsWith("X64"))
        {
            header.SetResourceArch(Arch.x64);
            return "BPR";
        }
        return format;
    }

    public static TextureHeaderBase? ConstructHeader(string Path, string Format, bool Verbose = true) 
    {
        // TODO: set x64 bool when constructing x64
        if (Verbose) Console.WriteLine($"Constructing {Format} texture property data...");
        return Format switch
        {
            "BPR" => new TextureHeaderBPR(Path),
            "BPRX64" => new TextureHeaderBPR(Path),
            "TUB" => new TextureHeaderPC(Path),
            "X360" => new TextureHeaderX360(Path),
            "PS3" => new TextureHeaderPS3(Path),
            _ => throw new InvalidPlatformException(),
        };
    }

    public static void CopyBaseClassProperties(TextureHeaderBase source, TextureHeaderBase destination)
    {
        var properties = typeof(TextureHeaderBase).GetProperties();
        foreach (var prop in properties)
        {
            var value = prop.GetValue(source);
            prop.SetValue(destination, value);
        }
    }

    public bool TryConvertTexture(int width, int height, int mipCount, string technique, int inFormat, int outFormat, string inPath, string outPath)
    {
        byte[] texture = File.ReadAllBytes(inPath);
        switch (technique)
        {
            case "PS3>>BPR":
                if ((CELL_GCM_COLOR_FORMAT)inFormat == CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_A8R8G8B8
                && (DXGI_FORMAT)outFormat == DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM)
                    DDSTextureUtilities.A8R8G8B8toB8G8R8A8(texture, width, height, mipCount);
                break;
            case "TUB>>BPR":
                if ((D3DFORMAT)inFormat == D3DFORMAT.D3DFMT_A8R8G8B8 
                && (DXGI_FORMAT)outFormat == DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM)
                    DDSTextureUtilities.A8R8G8B8toB8G8R8A8(texture, width, height, mipCount);
                if ((D3DFORMAT)inFormat == D3DFORMAT.D3DFMT_A8B8G8R8 
                && (DXGI_FORMAT)outFormat == DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM)
                    DDSTextureUtilities.A8B8G8R8toB8G8R8A8(texture, width, height, mipCount);
                break;
            default:
                texture = [];
                return false;
        };
        File.WriteAllBytes(outPath, texture);
        return true;
    }

    private static Dictionary<TKey, TValue> GetMapping<TKey, TValue>(string technique)
    {
        return technique switch
        {
            "PS3>>X360" => PS3toX360Mapping as Dictionary<TKey, TValue>,
            "X360>>PS3" => X360toPS3Mapping as Dictionary<TKey, TValue>,
            "TUB>>BPR" => TUBtoBPRMapping as Dictionary<TKey, TValue>,
            "BPR>>TUB" => BPRtoTUBMapping as Dictionary<TKey, TValue>,
            "PS3>>BPR" => PS3toBPRMapping as Dictionary<TKey, TValue>,
            "PS3>>TUB" => PS3toTUBMapping as Dictionary<TKey, TValue>,
            "X360>>TUB" => X360toTUBMapping as Dictionary<TKey, TValue>,
            "X360>>BPR" => X360toBPRMapping as Dictionary<TKey, TValue>,
            "TUB>>X360" => TUBtoX360Mapping as Dictionary<TKey, TValue>,
            "BPR>>PS3" => BPRtoPS3Mapping as Dictionary<TKey, TValue>,
            "BPR>>X360" => BPRtoX360Mapping as Dictionary<TKey, TValue>,
            _ => throw new ArgumentException("Invalid technique specified", nameof(technique))
        };
    }

    private static readonly Dictionary<GPUTEXTUREFORMAT, D3DFORMAT> X360toTUBMapping = new()
    {
        { GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT1, D3DFORMAT.D3DFMT_DXT1 },
        { GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT2_3, D3DFORMAT.D3DFMT_DXT3 },
        { GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT4_5, D3DFORMAT.D3DFMT_DXT5 },
        { GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_8, D3DFORMAT.D3DFMT_A8 },
        { GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_16_16_16_16, D3DFORMAT.D3DFMT_A16B16G16R16 },
        { GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_8_8_8_8, D3DFORMAT.D3DFMT_A8R8G8B8 },
        // TODO: Add more mappings
    };

    private static readonly Dictionary<GPUTEXTUREFORMAT, DXGI_FORMAT> X360toBPRMapping = new()
    {
        { GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT1, DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM },
        { GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT2_3, DXGI_FORMAT.DXGI_FORMAT_BC2_UNORM },
        { GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT4_5, DXGI_FORMAT.DXGI_FORMAT_BC3_UNORM },
        { GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_8, DXGI_FORMAT.DXGI_FORMAT_A8_UNORM },
        { GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_16_16_16_16, DXGI_FORMAT.DXGI_FORMAT_R16G16B16A16_UNORM },
        { GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_8_8_8_8, DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM },
        // TODO: Add more mappings
    };

    private static readonly Dictionary<D3DFORMAT, DXGI_FORMAT> TUBtoBPRMapping = new()
    {
        { D3DFORMAT.D3DFMT_DXT1, DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM },
        { D3DFORMAT.D3DFMT_DXT3, DXGI_FORMAT.DXGI_FORMAT_BC2_UNORM },
        { D3DFORMAT.D3DFMT_DXT5, DXGI_FORMAT.DXGI_FORMAT_BC3_UNORM },
        { D3DFORMAT.D3DFMT_A8, DXGI_FORMAT.DXGI_FORMAT_A8_UNORM },
        { D3DFORMAT.D3DFMT_A8B8G8R8, DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM },  // Does not directly match without converting!
        { D3DFORMAT.D3DFMT_A8R8G8B8, DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM },  // Does not directly match without converting!
        // TODO: Add more mappings
    };

    private static readonly Dictionary<DXGI_FORMAT, D3DFORMAT> BPRtoTUBMapping = new()
    {
        { DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM, D3DFORMAT.D3DFMT_DXT1 },
        { DXGI_FORMAT.DXGI_FORMAT_BC2_UNORM, D3DFORMAT.D3DFMT_DXT3 },
        { DXGI_FORMAT.DXGI_FORMAT_BC3_UNORM, D3DFORMAT.D3DFMT_DXT5 },
        { DXGI_FORMAT.DXGI_FORMAT_A8_UNORM, D3DFORMAT.D3DFMT_A8 },
        { DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM, D3DFORMAT.D3DFMT_A8B8G8R8 },  // Does not directly match without converting!
        // TODO: Add more mappings
    };

    private static readonly Dictionary<CELL_GCM_COLOR_FORMAT, D3DFORMAT> PS3toTUBMapping = new()
    {
        { CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_COMPRESSED_DXT1, D3DFORMAT.D3DFMT_DXT1 },
        { CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_COMPRESSED_DXT23, D3DFORMAT.D3DFMT_DXT3 },
        { CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_COMPRESSED_DXT45, D3DFORMAT.D3DFMT_DXT5 },
        { CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_B8, D3DFORMAT.D3DFMT_A8 },
        { CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_A8R8G8B8, D3DFORMAT.D3DFMT_A8R8G8B8 },
        // TODO: Add more mappings
    };

    private static readonly Dictionary<CELL_GCM_COLOR_FORMAT, GPUTEXTUREFORMAT> PS3toX360Mapping = new()
    {
        { CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_COMPRESSED_DXT1, GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT1 },
        { CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_COMPRESSED_DXT23, GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT2_3 },
        { CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_COMPRESSED_DXT45, GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT4_5 },
        { CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_B8, GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_8_B },
        { CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_A8R8G8B8, GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_8_8_8_8 },
        // TODO: Add more mappings
    };

    private static readonly Dictionary<D3DFORMAT, GPUTEXTUREFORMAT> TUBtoX360Mapping = new()
    {
        { D3DFORMAT.D3DFMT_DXT1, GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT1 },
        { D3DFORMAT.D3DFMT_DXT3, GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT2_3 },
        { D3DFORMAT.D3DFMT_DXT5, GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT4_5 },
        // TODO: Add more mappings
    };

    private static readonly Dictionary<GPUTEXTUREFORMAT, CELL_GCM_COLOR_FORMAT> X360toPS3Mapping = new()
    {
        { GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT1, CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_COMPRESSED_DXT1 },
        { GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT2_3, CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_COMPRESSED_DXT23 },
        { GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT4_5, CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_COMPRESSED_DXT45 },
        { GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_8_B, CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_B8 },
        { GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_8_8_8_8, CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_A8R8G8B8 },
        // TODO: Add more mappings
    };

    private static readonly Dictionary<CELL_GCM_COLOR_FORMAT, DXGI_FORMAT> PS3toBPRMapping = new()
    {
        { CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_COMPRESSED_DXT1, DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM },
        { CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_COMPRESSED_DXT23, DXGI_FORMAT.DXGI_FORMAT_BC2_UNORM },
        { CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_COMPRESSED_DXT45, DXGI_FORMAT.DXGI_FORMAT_BC3_UNORM },
        { CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_B8, DXGI_FORMAT.DXGI_FORMAT_A8_UNORM },
        { CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_A8R8G8B8, DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM }
        // TODO: Add more mappings
    };
    
    private static readonly Dictionary<DXGI_FORMAT, CELL_GCM_COLOR_FORMAT> BPRtoPS3Mapping = new()
    {
        { DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM, CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_COMPRESSED_DXT1 },
        { DXGI_FORMAT.DXGI_FORMAT_BC2_UNORM, CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_COMPRESSED_DXT23 },
        { DXGI_FORMAT.DXGI_FORMAT_BC3_UNORM, CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_COMPRESSED_DXT45 },
        { DXGI_FORMAT.DXGI_FORMAT_A8_UNORM, CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_B8 },
        // TODO: Add more mappings
    };

    private static readonly Dictionary<DXGI_FORMAT, GPUTEXTUREFORMAT> BPRtoX360Mapping = new()
    {
        { DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM, GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT1 },
        { DXGI_FORMAT.DXGI_FORMAT_BC2_UNORM, GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT2_3 },
        { DXGI_FORMAT.DXGI_FORMAT_BC3_UNORM, GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT4_5 },
        // TODO: Add more mappings
    };

    public PortTextureCommand() { }
}

