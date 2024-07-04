using Volatility.Resource.TextureHeader;
using Volatility.Utilities;

using static Volatility.Utilities.CgsIDUtilities;

namespace Volatility.CLI.Commands;

internal class PortTextureCommand : ICommand
{
    public static string CommandToken => "PortTexture";
    public static string CommandDescription => "Ports texture data from the specified source format to the specified destination format." +
        " NOTE: TUB & BPR format options are for the PC releases of the title.";
    public static string CommandParameters => "[--verbose] --informat=<tub,bpr[x64],x360,ps3> --inpath=<file/folder path> --outformat=<tub,bpr[x64],x360,ps3> [--outpath=<file/folder path>]";

    public string? SourceFormat { get; set; }
    public string? SourcePath { get; set; }
    public string? DestinationFormat { get; set; }
    public string? DestinationPath { get; set; }
    public bool Verbose { get; set; }

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
                        break;
                    case "X360>>PS3":
                        X360toPS3Mapping.TryGetValue((SourceTexture as TextureHeaderX360).Format.DataFormat, out CELL_GCM_COLOR_FORMAT x360ps3Format);
                        (DestinationTexture as TextureHeaderPS3).Format = x360ps3Format;
                        flipEndian = false;
                        break;

                    // ==== PC/BPR <> PC/BPR (no endian flip)

                    case "TUB>>BPR":
                        TUBToBPRMapping.TryGetValue((SourceTexture as TextureHeaderPC).Format, out DXGI_FORMAT tubbprFormat);
                        (DestinationTexture as TextureHeaderBPR).Format = tubbprFormat;
                        break;
                    case "BPR>>TUB":
                        BPRtoTUBMapping.TryGetValue((SourceTexture as TextureHeaderBPR).Format, out D3DFORMAT bprtubFormat);
                        (DestinationTexture as TextureHeaderPC).Format = bprtubFormat;
                        break;

                    // ==== Console <> PC/BPR (endian flip)

                    // = PS3 Source
                    case "PS3>>BPR":
                        PS3toBPRMapping.TryGetValue((SourceTexture as TextureHeaderPS3).Format, out DXGI_FORMAT ps3bprFormat);
                        (DestinationTexture as TextureHeaderBPR).Format = ps3bprFormat;
                        flipEndian = true;
                        break;
                    case "PS3>>TUB":
                        PS3toTUBMapping.TryGetValue((SourceTexture as TextureHeaderPS3).Format, out D3DFORMAT ps3tubFormat);
                        (DestinationTexture as TextureHeaderPC).Format = ps3tubFormat;
                        flipEndian = true;
                        break;

                    // = X360 Source
                    case "X360>>TUB":
                        X360ToTUBMapping.TryGetValue((SourceTexture as TextureHeaderX360).Format.DataFormat, out D3DFORMAT x360tubFormat);
                        (DestinationTexture as TextureHeaderPC).Format = x360tubFormat;
                        flipEndian = true;
                        break;
                    case "X360>>BPR":
                        X360ToBPRMapping.TryGetValue((SourceTexture as TextureHeaderX360).Format.DataFormat, out DXGI_FORMAT x360bprFormat);
                        (DestinationTexture as TextureHeaderBPR).Format = x360bprFormat;
                        flipEndian = true;
                        break;

                    // = TUB Source
                    case "TUB>>X360":
                        TUBtoX360Mapping.TryGetValue((SourceTexture as TextureHeaderPC).Format, out GPUTEXTUREFORMAT tubx360Format);
                        (DestinationTexture as TextureHeaderX360).Format.DataFormat = tubx360Format;
                        flipEndian = true;
                        break;

                    default:
                        throw new NotImplementedException($"Conversion technique {technique} is not yet implemented.");
                };

                // Finalize Destination
                DestinationTexture.PushAll();

                string outCgsFilename = flipEndian ? FlipPathCgsIDEndian(Path.GetFileName(sourceFile)) : Path.GetFileName(sourceFile);

                string outPath = @"";

                if (DestinationPath == sourceFile)
                {
                    outPath = $"{Path.GetDirectoryName(DestinationPath)}{Path.DirectorySeparatorChar}{outCgsFilename}";
                }
                // If we're outputting to a directory
                else if (new DirectoryInfo(DestinationPath).Exists)
                {
                    outPath = DestinationPath + Path.DirectorySeparatorChar + outCgsFilename;
                }

                // Detile bitmap data
                string sourceBitmapPath = $"{Path.GetDirectoryName(sourceFile)}{Path.DirectorySeparatorChar}{Path.GetFileNameWithoutExtension(sourceFile)}_texture.dat";
                string destinationBitmapPath = $"{Path.GetDirectoryName(outPath)}{Path.DirectorySeparatorChar}{Path.GetFileNameWithoutExtension(outPath)}_texture.dat";

                try
                {
                    // Currently requires an external tool. Every texture I've encountered on PS3 is
                    // already raw DDS anyway, so there's not really any reason to do this as far as I see.

                    // if (SourceTexture is TextureHeaderPS3 && DestinationTexture is not TextureHeaderPS3)
                    // {
                    //     PS3TextureUtilities.PS3GTFToDDS(SourcePath, sourceBitmapPath, destinationBitmapPath, Verbose);
                    // }

                    if (DestinationTexture is TextureHeaderX360 destX && SourceTexture is not TextureHeaderX360)
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
                    else if (SourceTexture is TextureHeaderX360 sourceX)
                    {
                        if (sourceX.Format.Tiled && !string.IsNullOrEmpty(sourceBitmapPath))
                        {
                            if (Verbose) Console.WriteLine($"Detiling X360 bitmap data for {Path.GetDirectoryName(outPath)}{Path.DirectorySeparatorChar}{Path.GetFileNameWithoutExtension(outPath)}_texture.dat...");
                            X360TextureUtilities.WriteUntiled360TextureFile(sourceX, sourceBitmapPath, destinationBitmapPath);
                        }
                    }
                    else
                    {
                        if (Verbose) Console.WriteLine($"Copying associated bitmap data for {Path.GetDirectoryName(outPath)}{Path.DirectorySeparatorChar}{Path.GetFileNameWithoutExtension(outPath)}_texture.dat...");
                        File.Copy(sourceBitmapPath, destinationBitmapPath, true);
                    }
                    if (Verbose) Console.WriteLine($"Wrote texture bitmap data to {DestinationFormat} destination directory.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error trying to copy bitmap data for {Path.GetFileNameWithoutExtension(sourceFile)}: {ex.Message}");
                }

                // Write header data (now after bitmap data to ensure any final edits are included)
                using FileStream fs = new(outPath, FileMode.Create, FileAccess.Write);
                using (BinaryWriter writer = new(fs))
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
    }

    public string BPRx64Hack(TextureHeaderBase header, string format)
    {
        if (header is TextureHeaderBPR && format.EndsWith("x64"))
        {
            (header as TextureHeaderBPR).x64Header = true;
            return "BPR";
        }
        return format;
    }

    public static TextureHeaderBase? ConstructHeader(string Path, string Format, bool Verbose = true) 
    {
        if (Verbose) Console.WriteLine($"Constructing {Format} texture property data...");
        return Format switch
        {
            "BPR" => new TextureHeaderBPR(Path),
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

    private static readonly Dictionary<GPUTEXTUREFORMAT, D3DFORMAT> X360ToTUBMapping = new()
    {
        { GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT1, D3DFORMAT.D3DFMT_DXT1 },
        { GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT2_3, D3DFORMAT.D3DFMT_DXT3 },
        { GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT4_5, D3DFORMAT.D3DFMT_DXT5 },
        { GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_8, D3DFORMAT.D3DFMT_A8 },
        { GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_16_16_16_16, D3DFORMAT.D3DFMT_A16B16G16R16 },
        { GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_8_8_8_8, D3DFORMAT.D3DFMT_A8R8G8B8 },
        // TODO: Add more mappings
    };

    private static readonly Dictionary<GPUTEXTUREFORMAT, DXGI_FORMAT> X360ToBPRMapping = new()
    {
        { GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT1, DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM },
        { GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT2_3, DXGI_FORMAT.DXGI_FORMAT_BC2_UNORM },
        { GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT4_5, DXGI_FORMAT.DXGI_FORMAT_BC3_UNORM },
        { GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_8, DXGI_FORMAT.DXGI_FORMAT_A8_UNORM },
        { GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_16_16_16_16, DXGI_FORMAT.DXGI_FORMAT_R16G16B16A16_UNORM },
        { GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_8_8_8_8, DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM },
        // TODO: Add more mappings
    };

    private static readonly Dictionary<D3DFORMAT, DXGI_FORMAT> TUBToBPRMapping = new()
    {
        { D3DFORMAT.D3DFMT_DXT1, DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM },
        { D3DFORMAT.D3DFMT_DXT3, DXGI_FORMAT.DXGI_FORMAT_BC2_UNORM },
        { D3DFORMAT.D3DFMT_DXT5, DXGI_FORMAT.DXGI_FORMAT_BC3_UNORM },
        { D3DFORMAT.D3DFMT_A8, DXGI_FORMAT.DXGI_FORMAT_A8_UNORM },
        // TODO: Add more mappings
    };

    private static readonly Dictionary<DXGI_FORMAT, D3DFORMAT> BPRtoTUBMapping = new()
    {
        { DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM, D3DFORMAT.D3DFMT_DXT1 },
        { DXGI_FORMAT.DXGI_FORMAT_BC2_UNORM, D3DFORMAT.D3DFMT_DXT3 },
        { DXGI_FORMAT.DXGI_FORMAT_BC3_UNORM, D3DFORMAT.D3DFMT_DXT5 },
        { DXGI_FORMAT.DXGI_FORMAT_A8_UNORM, D3DFORMAT.D3DFMT_A8 },
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
        // TODO: Add more mappings
    };
}

