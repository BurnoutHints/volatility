using System.Reflection;

using Volatility.Resources;

using static Volatility.Utilities.DataUtilities;
using static Volatility.Utilities.ResourceIDUtilities;

namespace Volatility.CLI.Commands;

internal class AutotestCommand : ICommand
{
    public static string CommandToken => "autotest";
    public static string CommandDescription => "Runs automatic tests to ensure the application is working." +
        " When provided a path & format, will import, export, then reimport specified file to ensure IO parity.";
    public static string CommandParameters => "[--format=<tub,bpr,x360,ps3>] [--path=<file path>]";

    public string? Format { get; set; }
    public string? Path { get; set; }

    public async Task Execute()
    {
        if (!string.IsNullOrEmpty(Path))
        {
            TextureBase? header = Format switch
            {
                "BPR" => new TextureBPR(Path),
                "TUB" => new TexturePC(Path),
                "X360" => new TextureX360(Path),
                "PS3" => new TexturePS3(Path),
                _ => throw new InvalidPlatformException(),
            };

            header.PullAll();

            TestHeaderRW($"autotest_{System.IO.Path.GetFileName(Path)}", header);

            return;
        }

        /*
         * Right now, the autotest simply creates
         * example texture classes akin to what the parser
         * will interpret from an input format, then write
         * them out to various platform formatted header files.
         */
            
        // TUB Texture data test case
        TexturePC textureHeaderPC = new TexturePC
        {
            AssetName = "autotest_header_PC",
            ResourceID = GetResourceIDFromName("autotest_header_PC"),
            Format = D3DFORMAT.D3DFMT_DXT1,
            Width = 1024,
            Height = 512,
            MipmapLevels = 11,
            GRTexture = true
        };

        TestHeaderRW("autotest_header_PC.dat", textureHeaderPC);

        // BPR Texture data test case
        TextureBPR textureHeaderBPR = new TextureBPR
        {
            AssetName = "autotest_header_BPR",
            ResourceID = GetResourceIDFromName("autotest_header_BPR"),
            Format = DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM,
            Width = 1024,
            Height = 512,
            MipmapLevels = 11,
            GRTexture = true
        };

        // SKIPPING BPR IMPORT AS IT'S NOT SUPPORTED YET

        // Write 32 bit test BPR header
        TestHeaderRW("autotest_header_BPR.dat", textureHeaderBPR);

        textureHeaderBPR.SetResourceArch(Arch.x64);
        textureHeaderBPR.AssetName = "autotest_header_BPRx64";
        textureHeaderBPR.ResourceID = GetResourceIDFromName(textureHeaderBPR.AssetName);

        // Write 64 bit test BPR header
        TestHeaderRW("autotest_header_BPRx64.dat", textureHeaderBPR);

        // PS3 Texture data test case
        TexturePS3 textureHeaderPS3 = new TexturePS3
        {
            AssetName = "autotest_header_PS3",
            ResourceID = GetResourceIDFromName("autotest_header_PS3"),
            Format = CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_COMPRESSED_DXT45,
            Width = 1024,
            Height = 512,
            MipmapLevels = 11,
            GRTexture = true
        };
        textureHeaderPS3.PushAll();
        TestHeaderRW("autotest_header_PS3.dat", textureHeaderPS3);

        // X360 Texture data test case
        TextureX360 textureHeaderX360 = new TextureX360
        {
            AssetName = "autotest_header_X360",
            ResourceID = GetResourceIDFromName("autotest_header_X360"),
            Format = new GPUTEXTURE_FETCH_CONSTANT
            {
                Tiled = true,
                SwizzleW = GPUSWIZZLE.GPUSWIZZLE_W,
                SwizzleX = GPUSWIZZLE.GPUSWIZZLE_X,
                SwizzleY = GPUSWIZZLE.GPUSWIZZLE_Y,
                SwizzleZ = GPUSWIZZLE.GPUSWIZZLE_Z,
            },
            Width = 1024,
            Height = 512,
            Depth = 1,
            MipmapLevels = 11,
            GRTexture = true
        };
        textureHeaderX360.PushAll();
        TestHeaderRW("autotest_header_X360.dat", textureHeaderX360);

        // File name endian flip test case
        string endianFlipTestName = "12_34_56_78_texture.dat";
        Console.WriteLine($"AUTOTEST - Endian Test: Flipped endian {endianFlipTestName} to {FlipPathResourceIDEndian(endianFlipTestName)}");
    }

    public void SetArgs(Dictionary<string, object> args)
    {
        Format = (args.TryGetValue("format", out object? format) ? format as string : "auto").ToUpper();
        Path = args.TryGetValue("path", out object? path) ? path as string : "";
    }

    public void TestHeaderRW(string name, TextureBase header, bool skipImport = false) 
    {
        using (FileStream fs = new FileStream(name, FileMode.Create))
        {
            // We don't want the command runner to catch the error
            try
            {
                header.PushAll();
            }
            catch (NotImplementedException)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"A push isn't implemented for {header.GetType().Name}!");
                Console.ResetColor();
            }

            using (BinaryWriter writer = new(fs))
            {
                Console.WriteLine($"AUTOTEST - Writing autotest {name} to working directory...");
                header.WriteToStream(writer, header.GetResourceEndian());
                writer.Close();
            }

            if (skipImport)
                return;
            
            TextureBase? newHeader = System.ComponentModel.TypeDescriptor.CreateInstance(
                                provider: null,
                                objectType: header.GetType(),
                                argTypes: [typeof(string)],
                                args: new object[] { fs.Name }) as TextureBase;

            try
            {
                newHeader?.PullAll();
            }
            catch (NotImplementedException)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"A pull isn't implemented for {newHeader?.GetType().Name}!");
                Console.ResetColor();
            }

            TestCompareHeaders(header, newHeader);
        }
    }

    public static void TestCompareHeaders(object exported, object imported)
    {
        Type type = exported.GetType();

        Console.WriteLine(">> Comparing properties and fields of " + type.Name + ":");
    
        PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        int mismatches = 0;
        foreach (PropertyInfo property in properties)
        {
            
            object value1 = property.GetValue(exported, null);
            object value2 = property.GetValue(imported, null);
    
            if (IsComplexType(property.PropertyType))
            {
                Console.WriteLine($" >  Inspecting nested type {property.Name}:");
                TestCompareHeaders(value1, value2);
                Console.WriteLine($" >  Finished inspecting nested type {property.Name}");
            }
            else if (!Equals(value1, value2))
            {
                mismatches++;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Mismatch - {property.Name}: Exported = {value1}, Imported = {value2}");
                Console.ResetColor();
            }
        }
    
        FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
        foreach (FieldInfo field in fields)
        {
            object value1 = field.GetValue(exported);
            object value2 = field.GetValue(imported);
    
            if (IsComplexType(field.FieldType))
            {
                Console.WriteLine($" >  Inspecting nested type {field.Name}:");
                TestCompareHeaders(value1, value2);
                Console.WriteLine($" >  Finished inspecting nested type {field.Name}");
            }
            else if (!Equals(value1, value2))
            {
                mismatches++;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Mismatch - {field.Name}: Exported = {value1}, Imported = {value2}");
                Console.ResetColor();
            }
        }

        if (mismatches == 0) 
            Console.ForegroundColor = ConsoleColor.Green;

        Console.WriteLine(">> Finished Comparing properties and fields of " + type.Name + $" - {mismatches} mismatches");
        Console.ResetColor();
    }

    public AutotestCommand() { }
}