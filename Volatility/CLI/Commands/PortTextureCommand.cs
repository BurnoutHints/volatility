
using Volatility.TextureHeader;
using Volatility.Utilities;

using static Volatility.Utilities.DataUtilities;

namespace Volatility.CLI.Commands;

internal class PortTextureCommand : ICommand
{
    public string CommandToken => "PortTexture";
    public string CommandDescription => "Ports texture data from the specified source format to the specified destination format." +
        "\nNOTE: TUB & BPR format options are for the PC releases of the title.";
    public string CommandParameters => "--informat=<tub,bpr,x360,ps3> --inpath=<file path> --outformat=<tub,bpr,x360,ps3> --outpath=<file path>";

    public string? SourceFormat { get; set; }
    public string? SourcePath { get; set; }
    public string? DestinationFormat { get; set; }
    public string? DestinationPath { get; set; }

    public void Execute()
    {
        var SourceTexture = ConstructHeader(SourcePath, SourceFormat);
        var DestinationTexture = ConstructHeader(DestinationPath, DestinationFormat);

        SourceTexture.PullAll();

        CopyBaseClassProperties(SourceTexture, DestinationTexture);

        // Manual header format conversion
        switch ($"{SourceFormat}>>{DestinationFormat}")
        {
            case "X360>>TUB":
                ((TextureHeaderPC)DestinationTexture).Format = GPUTEXTUREFORMATtoD3DFORMAT(((TextureHeaderX360)SourceTexture).Format.DataFormat);
                break;
        };

        // Finalize Destination
        DestinationTexture.PushAll();

        if (DestinationPath == SourcePath)
        {
            DestinationPath = $"{Path.GetDirectoryName(DestinationPath)}{Path.DirectorySeparatorChar}{FlipFileNameEndian(Path.GetFileName(DestinationPath))}";
        }

        using FileStream fs = new FileStream(DestinationPath, FileMode.Create, FileAccess.Write);
        using (BinaryWriter writer = new BinaryWriter(fs))
        {
            Console.WriteLine($"Writing converted texture property data to destination...");
            DestinationTexture.WriteToStream(writer);
        }

        // Detile bitmap data
        string sourceBitmapPath = $"{Path.GetDirectoryName(SourcePath)}{Path.DirectorySeparatorChar}{Path.GetFileNameWithoutExtension(SourcePath)}_texture.dat";
        string destinationBitmapPath = $"{Path.GetDirectoryName(DestinationPath)}{Path.DirectorySeparatorChar}{Path.GetFileNameWithoutExtension(DestinationPath)}_texture.dat";

        try
        {
            if (((TextureHeaderX360)SourceTexture).Format.Tiled && !string.IsNullOrEmpty(sourceBitmapPath))
            {
                Console.WriteLine($"Detiling X360 bitmap data for {Path.GetDirectoryName(DestinationPath)}{Path.DirectorySeparatorChar}{Path.GetFileNameWithoutExtension(DestinationPath)}_texture.dat...");
                X360TextureUtilities.WriteUntiled360TextureFile((TextureHeaderX360)SourceTexture, sourceBitmapPath, destinationBitmapPath);
            }
            else
            {
                Console.WriteLine($"Writing associated bitmap data for {Path.GetDirectoryName(DestinationPath)}{Path.DirectorySeparatorChar}{Path.GetFileNameWithoutExtension(DestinationPath)}_texture.dat...");
                File.Copy(sourceBitmapPath, destinationBitmapPath, true);
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error trying to copy bitmap data for {Path.GetFileNameWithoutExtension(SourcePath)}: {ex.Message}");
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

    public static TextureHeaderBase? ConstructHeader(string Path, string Format) 
    {
        Console.WriteLine($"Reading {Format} texture property data...");
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


    private static readonly Dictionary<GPUTEXTUREFORMAT, D3DFORMAT> formatMappings = new Dictionary<GPUTEXTUREFORMAT, D3DFORMAT>
    {
        { GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT1, D3DFORMAT.D3DFMT_DXT1 },
        { GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT2_3, D3DFORMAT.D3DFMT_DXT3 },
        { GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT4_5, D3DFORMAT.D3DFMT_DXT5 },
        { GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_8, D3DFORMAT.D3DFMT_A8 },
        { GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_16_16_16_16, D3DFORMAT.D3DFMT_A16B16G16R16 },
        // TODO: Add more mappings
    };

    public static D3DFORMAT GPUTEXTUREFORMATtoD3DFORMAT(GPUTEXTUREFORMAT gpuFormat)
    {
        if (formatMappings.TryGetValue(gpuFormat, out D3DFORMAT d3dFormat))
        {
            return d3dFormat;
        }
        else
        {
            return D3DFORMAT.D3DFMT_UNKNOWN;  // Default case if no mapping is found
        }
    }
}

