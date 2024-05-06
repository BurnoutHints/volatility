
using Volatility.TextureHeader;
using Volatility.Utilities;

using static Volatility.Utilities.DataUtilities;

namespace Volatility.CLI.Commands;

internal class PortTextureCommand : ICommand
{
    public string CommandToken => "PortTexture";
    public string CommandDescription => "Ports texture data from the specified source format to the specified destination format." +
        "\nNOTE: TUB & BPR format options are for the PC releases of the title.";
    public string CommandParameters => "--informat=<tub,bpr[x64],x360,ps3> --inpath=<file/folder path> --outformat=<tub,bpr[x64],x360,ps3> [--outpath=<file/folder path>]";

    public string? SourceFormat { get; set; }
    public string? SourcePath { get; set; }
    public string? DestinationFormat { get; set; }
    public string? DestinationPath { get; set; }

    public void Execute()
    {
        string[] files = new string[]
        {
            SourcePath
        };
        if (new DirectoryInfo(SourcePath).Exists)
        {
            List<string> f = Directory.GetFiles(SourcePath).ToList();
            for (int i = 0; i < f.Count(); i++) 
            {
                var name = Path.GetFileName(f[i]);
                if (!name.Contains(".dat") || name.Contains("_texture"))
                {
                    f.Remove(f[i]);
                }
            }
            files = f.ToArray();
        }

        foreach (string sourceFile in files)
        { 
            TextureHeaderBase SourceTexture = ConstructHeader(sourceFile, SourceFormat);
            TextureHeaderBase DestinationTexture = ConstructHeader(DestinationPath, DestinationFormat);
            
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
                    (DestinationTexture as TextureHeaderX360).Format.DataFormat = ps3x360Format;
                    break;
                case "X360>>PS3":
                    X360toPS3Mapping.TryGetValue((SourceTexture as TextureHeaderX360).Format.DataFormat, out CELL_GCM_COLOR_FORMAT x360ps3Format);
                    (DestinationTexture as TextureHeaderPS3).Format = x360ps3Format;
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

            string outCgsFilename = flipEndian ? FlipFileNameEndian(Path.GetFileName(sourceFile)) : Path.GetFileName(sourceFile);

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

            using FileStream fs = new FileStream(outPath, FileMode.Create, FileAccess.Write);
            using (BinaryWriter writer = new BinaryWriter(fs))
            {
                Console.WriteLine($"Writing converted texture property data to destination file...");
                try 
                {
                    DestinationTexture.WriteToStream(writer);
                }
                catch 
                {
                    throw new IOException("Failed to write converted texture property data to stream.");
                }
            }

            // Detile bitmap data
            string sourceBitmapPath = $"{Path.GetDirectoryName(sourceFile)}{Path.DirectorySeparatorChar}{Path.GetFileNameWithoutExtension(sourceFile)}_texture.dat";
            string destinationBitmapPath = $"{Path.GetDirectoryName(outPath)}{Path.DirectorySeparatorChar}{Path.GetFileNameWithoutExtension(outPath)}_texture.dat";

            try
            {
                if (SourceTexture is TextureHeaderX360)
                {
                    if ((SourceTexture as TextureHeaderX360).Format.Tiled && !string.IsNullOrEmpty(sourceBitmapPath)) 
                    {
                        Console.WriteLine($"Detiling X360 bitmap data for {Path.GetDirectoryName(outPath)}{Path.DirectorySeparatorChar}{Path.GetFileNameWithoutExtension(outPath)}_texture.dat...");
                        X360TextureUtilities.WriteUntiled360TextureFile((TextureHeaderX360)SourceTexture, sourceBitmapPath, destinationBitmapPath);
                    }
                }
                else
                {
                    Console.WriteLine($"Copying associated bitmap data for {Path.GetDirectoryName(outPath)}{Path.DirectorySeparatorChar}{Path.GetFileNameWithoutExtension(outPath)}_texture.dat...");
                    File.Copy(sourceBitmapPath, destinationBitmapPath, true);
                }
                Console.WriteLine($"Wrote texture bitmap data to destination directory.");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error trying to copy bitmap data for {Path.GetFileNameWithoutExtension(sourceFile)}: {ex.Message}");
                Console.ResetColor();
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Successfully ported {SourceFormat} formatted {Path.GetFileNameWithoutExtension(sourceFile)} to {DestinationFormat} as {Path.GetFileNameWithoutExtension(outPath)}.");
            Console.ResetColor();
        }
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

    public static TextureHeaderBase? ConstructHeader(string Path, string Format) 
    {
        Console.WriteLine($"Constructing {Format} texture property data...");
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

    private static readonly Dictionary<GPUTEXTUREFORMAT, D3DFORMAT> X360ToTUBMapping = new Dictionary<GPUTEXTUREFORMAT, D3DFORMAT>
    {
        { GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT1, D3DFORMAT.D3DFMT_DXT1 },
        { GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT2_3, D3DFORMAT.D3DFMT_DXT3 },
        { GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT4_5, D3DFORMAT.D3DFMT_DXT5 },
        { GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_8, D3DFORMAT.D3DFMT_A8 },
        { GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_16_16_16_16, D3DFORMAT.D3DFMT_A16B16G16R16 },
        { GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_8_8_8_8, D3DFORMAT.D3DFMT_A8R8G8B8 },
        // TODO: Add more mappings
    };

    private static readonly Dictionary<GPUTEXTUREFORMAT, DXGI_FORMAT> X360ToBPRMapping = new Dictionary<GPUTEXTUREFORMAT, DXGI_FORMAT>
    {
        { GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT1, DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM },
        { GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT2_3, DXGI_FORMAT.DXGI_FORMAT_BC2_UNORM },
        { GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT4_5, DXGI_FORMAT.DXGI_FORMAT_BC3_UNORM },
        { GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_8, DXGI_FORMAT.DXGI_FORMAT_A8_UNORM },
        { GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_16_16_16_16, DXGI_FORMAT.DXGI_FORMAT_R16G16B16A16_UNORM },
        { GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_8_8_8_8, DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM },
        // TODO: Add more mappings
    };

    private static readonly Dictionary<D3DFORMAT, DXGI_FORMAT> TUBToBPRMapping = new Dictionary<D3DFORMAT, DXGI_FORMAT>
    {
        { D3DFORMAT.D3DFMT_DXT1, DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM },
        { D3DFORMAT.D3DFMT_DXT3, DXGI_FORMAT.DXGI_FORMAT_BC2_UNORM },
        { D3DFORMAT.D3DFMT_DXT5, DXGI_FORMAT.DXGI_FORMAT_BC3_UNORM },
        { D3DFORMAT.D3DFMT_A8, DXGI_FORMAT.DXGI_FORMAT_A8_UNORM },
        // TODO: Add more mappings
    };

    private static readonly Dictionary<DXGI_FORMAT, D3DFORMAT> BPRtoTUBMapping = new Dictionary<DXGI_FORMAT, D3DFORMAT>
    {
        { DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM, D3DFORMAT.D3DFMT_DXT1 },
        { DXGI_FORMAT.DXGI_FORMAT_BC2_UNORM, D3DFORMAT.D3DFMT_DXT3 },
        { DXGI_FORMAT.DXGI_FORMAT_BC3_UNORM, D3DFORMAT.D3DFMT_DXT5 },
        { DXGI_FORMAT.DXGI_FORMAT_A8_UNORM, D3DFORMAT.D3DFMT_A8 },
        // TODO: Add more mappings
    };

    private static readonly Dictionary<CELL_GCM_COLOR_FORMAT, D3DFORMAT> PS3toTUBMapping = new Dictionary<CELL_GCM_COLOR_FORMAT, D3DFORMAT>
    {
        { CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_COMPRESSED_DXT1, D3DFORMAT.D3DFMT_DXT1 },
        { CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_COMPRESSED_DXT23, D3DFORMAT.D3DFMT_DXT3 },
        { CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_COMPRESSED_DXT45, D3DFORMAT.D3DFMT_DXT5 },
        { CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_B8, D3DFORMAT.D3DFMT_A8 },
        { CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_A8R8G8B8, D3DFORMAT.D3DFMT_A8R8G8B8 },
        // TODO: Add more mappings
    };

    private static readonly Dictionary<CELL_GCM_COLOR_FORMAT, GPUTEXTUREFORMAT> PS3toX360Mapping = new Dictionary<CELL_GCM_COLOR_FORMAT, GPUTEXTUREFORMAT>
    {
        { CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_COMPRESSED_DXT1, GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT1 },
        { CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_COMPRESSED_DXT23, GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT2_3 },
        { CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_COMPRESSED_DXT45, GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT4_5 },
        { CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_B8, GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_8_B },
        { CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_A8R8G8B8, GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_8_8_8_8 },
        // TODO: Add more mappings
    };

    private static readonly Dictionary<D3DFORMAT, GPUTEXTUREFORMAT> TUBtoX360Mapping = new Dictionary<D3DFORMAT, GPUTEXTUREFORMAT>
    {
        { D3DFORMAT.D3DFMT_DXT1, GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT1 },
        { D3DFORMAT.D3DFMT_DXT3, GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT2_3 },
        { D3DFORMAT.D3DFMT_DXT5, GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT4_5 },
        // TODO: Add more mappings
    };

    private static readonly Dictionary<GPUTEXTUREFORMAT, CELL_GCM_COLOR_FORMAT> X360toPS3Mapping = new Dictionary<GPUTEXTUREFORMAT, CELL_GCM_COLOR_FORMAT>
    {
        { GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT1, CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_COMPRESSED_DXT1 },
        { GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT2_3, CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_COMPRESSED_DXT23 },
        { GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT4_5, CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_COMPRESSED_DXT45 },
        { GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_8_B, CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_B8 },
        { GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_8_8_8_8, CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_A8R8G8B8 },
        // TODO: Add more mappings
    };

    private static readonly Dictionary<CELL_GCM_COLOR_FORMAT, DXGI_FORMAT> PS3toBPRMapping = new Dictionary<CELL_GCM_COLOR_FORMAT, DXGI_FORMAT>
    {
        { CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_COMPRESSED_DXT1, DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM },
        { CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_COMPRESSED_DXT23, DXGI_FORMAT.DXGI_FORMAT_BC2_UNORM },
        { CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_COMPRESSED_DXT45, DXGI_FORMAT.DXGI_FORMAT_BC3_UNORM },
        { CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_B8, DXGI_FORMAT.DXGI_FORMAT_A8_UNORM },
        // TODO: Add more mappings
    };
}

