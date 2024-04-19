using Volatility.TextureHeader;

using System.Reflection;

namespace Volatility;

class ImportRawCommand : ICommand
{
    public string? Format { get; set; }
    public string? Path { get; set; }
    
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

        TextureHeaderBase? header = Format switch
        {
            "BPR" => new TextureHeaderBPR(Path),
            "TUB" => new TextureHeaderPC(Path),
            "X360" => new TextureHeaderX360(Path),
            "PS3" => new TextureHeaderPS3(Path),
            _ => throw new InvalidPlatformException(),
        };

        FileStream fileStream = new FileStream(Path, FileMode.Open, FileAccess.Read, FileShare.Read);

        using (BinaryReader reader = new BinaryReader(fileStream))
        {
            Console.WriteLine($"Constructing {Format} texture property data...");
            header.ParseFromStream(reader);
            header.PullAll();
        }

        fileStream.Close();

        Console.WriteLine($"Imported {Path}.");
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
            "Usage: ImportRaw --format=<tub,bpr,x360,ps3> --path=<file path>" +
            "\nNOTE: TUB & BPR format options are for the PC releases of the title."
        );
    }
}