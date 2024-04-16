using Volatility.TextureHeader;

using System.Reflection;

namespace Volatility;

class ImportRawCommand : ICommand
{
    public string Format { get; set; }
    public string Path { get; set; }
    
    public void Execute()
    {
        if (string.IsNullOrEmpty(Path))
        {
            Console.WriteLine("Error: No import path specified! (--path)");
            return;
        }

        FileAttributes fileAttributes;
        try
        {
            fileAttributes = File.GetAttributes(Path);
        }
        catch (FileNotFoundException)
        {
            Console.WriteLine("Error: Invalid file import path specified!");
            return;
        }
        catch (DirectoryNotFoundException)
        {
            Console.WriteLine("Error: Can not find directory for specified import path!");
            return;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error: Caught file exception {e.Message}.");
            return;
        }

        Console.WriteLine("Constructing texture property data...");

        TextureHeaderBase header;

        switch (Format)
        {
            case "bpr":
                header = new TextureHeaderBPR(Path);
                break;
            case "tub":
                header = new TextureHeaderPC(Path);
                break;
            case "x360":
                header = new TextureHeaderX360(Path);
                break;
            case "ps3":
                header = new TextureHeaderPS3(Path);
                break;
            case "auto":
                // TODO: Implement auto format detection/parsing
                Console.WriteLine("Error: Please specify a format! (--format)");
                return;
            case "":
            default:
                Console.WriteLine("Error: No input format specified! (--format)");
                return;
        }
        var bindingFlags = BindingFlags.Instance |
                   BindingFlags.NonPublic |
                   BindingFlags.Public;

        // List the fields for now to ensure we are getting the data.
        // TODO: Replace with proper serialized format.
        var fields = header.GetType().GetFields(bindingFlags).Select(field => field.GetValue(header)).ToList();
        Console.WriteLine(fields);
    }
    public void SetArgs(Dictionary<string, object> args)
    {
        Format = args.TryGetValue("format", out object? format) ? format as string : "auto";
        Path = args.TryGetValue("path", out object? path) ? path as string : "";
    }

    public void ShowUsage()
    {
        Console.WriteLine
        (
            "Usage: ImportRaw --format=<tub,bpr,x360,ps3> --path=<file path>" +
            "\nNOTE: TUB & BPR format options are for the PC releases of the title."
        );
    }
}