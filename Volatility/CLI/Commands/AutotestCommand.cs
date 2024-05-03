using System.Collections;
using System.Reflection;
using Volatility.TextureHeader;

using static Volatility.Utilities.DataUtilities;

namespace Volatility;

internal class AutotestCommand : ICommand
{
    public string? Format { get; set; }
    public string? Path { get; set; }

    public void Execute()
    {
        if (!string.IsNullOrEmpty(Path))
        {
            TextureHeaderBase? header = Format switch
            {
                "BPR" => new TextureHeaderBPR(Path),
                "TUB" => new TextureHeaderPC(Path),
                "X360" => new TextureHeaderX360(Path),
                "PS3" => new TextureHeaderPS3(Path),
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
        TextureHeaderPC textureHeaderPC = new TextureHeaderPC
        {
            Format = D3DFORMAT.D3DFMT_DXT1,
            Width = 1024,
            Height = 512,
            MipLevels = 11,
            GRTexture = true
        };

        TestHeaderRW("autotest_header_PC.dat", textureHeaderPC);

        // BPR Texture data test case
        TextureHeaderBPR textureHeaderBPR = new TextureHeaderBPR
        {
            Format = DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM,
            Width = 1024,
            Height = 512,
            MipLevels = 11,
            GRTexture = true
        };

        // SKIPPING BPR IMPORT AS IT'S NOT SUPPORTED YET

        // Write 32 bit test BPR header
        TestHeaderRW("autotest_header_BPR.dat", textureHeaderBPR, true);

        textureHeaderBPR.x64Header = true;

        // Write 64 bit test BPR header
        TestHeaderRW("autotest_header_BPRx64.dat", textureHeaderBPR, true);

        // PS3 Texture data test case
        TextureHeaderPS3 textureHeaderPS3 = new TextureHeaderPS3
        {
            Format = CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_COMPRESSED_DXT45,
            Width = 1024,
            Height = 512,
            MipmapLevels = 11,
            GRTexture = true
        };
        textureHeaderPS3.PushAll();
        TestHeaderRW("autotest_header_PS3.dat", textureHeaderPS3);

        // X360 Texture data test case
        TextureHeaderX360 textureHeaderX360 = new TextureHeaderX360
        {
            Format = new GPUTEXTURE_FETCH_CONSTANT
            {
                Size = new GPUTEXTURESIZE
                {
                    Width = 1024,
                    Height = 512,
                    Type = GPUTEXTURESIZE_TYPE.GPUTEXTURESIZE_2D
                },
                MaxMipLevel = 10,
                MinMipLevel = 0,
                Tiled = true,
            },
            GRTexture = true
        };
        textureHeaderX360.PushAll();
        TestHeaderRW("autotest_header_X360.dat", textureHeaderX360);

        // File name endian flip test case
        string endianFlipTestName = "12_34_56_78_texture.dat";
        Console.WriteLine($"Endian Test: Flipped endian {endianFlipTestName} to {FlipFileNameEndian(endianFlipTestName)}");
    }

    public void SetArgs(Dictionary<string, object> args)
    {
        Format = (args.TryGetValue("format", out object? format) ? format as string : "auto").ToUpper();
        Path = args.TryGetValue("path", out object? path) ? path as string : "";
    }

    public void ShowUsage()
    {
        Console.WriteLine
        (
            "Usage: autotest [--format=<tub,bpr,x360,ps3>] [--path=<file path>]" +
            "\nRuns a series of automatic tests to ensure the application is working correctly." +
            "\nWhen provided a path & format, will import, export, then reimport specified file to ensure IO parity."
        );
    }

    public void TestHeaderRW(string name, TextureHeaderBase header, bool skipImport = false) 
    {
        using (FileStream fs = new FileStream(name, FileMode.Create))
        {
            // Most aren't implemented and we don't
            // want the command runner to catch the error
            try
            {
                header.PushAll();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"Error with PushAll: {ex.Message}");
                Console.ResetColor();
            }

            using (BinaryWriter writer = new BinaryWriter(fs))
            {
                Console.WriteLine($"Writing autotest {name} to working directory...");
                header.WriteToStream(writer);
                writer.Close();
            }

            if (!skipImport)
            {
                Type type = header.GetType();

                var newHeaderObject = System.ComponentModel.TypeDescriptor.CreateInstance(
                                    provider: null,
                                    objectType: type,
                                    argTypes: new Type[] { Type.GetType("System.String") },
                                    args: new object[] { fs.Name });

                TextureHeaderBase? newHeader = (TextureHeaderBase?)newHeaderObject;

                try
                {
                    newHeader?.PullAll();
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine($"Error with PullAll: {ex.Message}");
                    Console.ResetColor();
                }

                TestCompareHeaders(header, newHeader);
            }
        }
    }

    public static void TestCompareHeaders(object exported, object imported)
    {
        Type type = exported.GetType();

        Console.WriteLine("==  Comparing properties and fields of " + type.Name + ":");
    
        PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (PropertyInfo property in properties)
        {
            object value1 = property.GetValue(exported, null);
            object value2 = property.GetValue(imported, null);
    
            if (IsComplexType(property.PropertyType))
            {
                Console.WriteLine($" = Inspecting nested type {property.Name}:");
                TestCompareHeaders(value1, value2);
                Console.WriteLine($" = Finished inspecting nested type {property.Name}");
            }
            else if (!Equals(value1, value2))
            {
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
                Console.WriteLine($" = Inspecting nested type {field.Name}:");
                TestCompareHeaders(value1, value2);
                Console.WriteLine($" = Finished inspecting nested type {field.Name}");
            }
            else if (!Equals(value1, value2))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Mismatch - {field.Name}: Exported = {value1}, Imported = {value2}");
                Console.ResetColor();
            }
        }
        Console.WriteLine("==  Finished Comparing properties and fields of " + type.Name);
    }
    public static bool IsComplexType(Type type)
    {
        return !type.IsPrimitive && !type.IsEnum && type != typeof(string) && !type.IsArray && type != typeof(BitArray);
    }
}