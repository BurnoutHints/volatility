using System.Reflection;
using Volatility.TextureHeader;

using static Volatility.Utilities.DataUtilities;


namespace Volatility.CLI.Commands;

internal class ImportRawCommand : ICommand
{
    public string CommandToken => "ImportRaw";
    public string CommandDescription => "NOTE: TUB & BPR format options are for the PC releases of the title.";
    public string CommandParameters => "--format=<tub,bpr,x360,ps3> --path=<file path>";

    public string? Format { get; set; }
    public string? Path { get; set; }

    public void Execute()
    {
        if (string.IsNullOrEmpty(Path))
        {
            Console.WriteLine("Error: No import path specified! (--path)");
            return;
        }

        foreach (string sourceFile in ICommand.GetFilesInDirectory(Path))
        {
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

            Console.WriteLine($"Constructing {Format} texture property data...");
            TextureHeaderBase? header = Format switch
            {
                "BPR" => new TextureHeaderBPR(Path),
                "TUB" => new TextureHeaderPC(Path),
                "X360" => new TextureHeaderX360(Path),
                "PS3" => new TextureHeaderPS3(Path),
                _ => throw new InvalidPlatformException(),
            };

            header.PullAll();

            SerializeFields(header);

            Console.WriteLine($"Imported {Path}.");
        }
    }
    public void SetArgs(Dictionary<string, object> args)
    {
        Format = (args.TryGetValue("format", out object? format) ? format as string : "auto").ToUpper();
        Path = args.TryGetValue("path", out object? path) ? path as string : "";
    }

    public static void SerializeFields(object exported, int tabs = 0)
    {
        string tabbed = new string('\t', tabs);

        Type type = exported.GetType();

        Console.Write($"{type.Name}\n{tabbed}{{\n");

        tabbed = new string('\t', ++tabs);

        PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (PropertyInfo property in properties)
        {
            object value1 = property.GetValue(exported, null);

            if (IsComplexType(property.PropertyType))
            {
                Console.Write(tabbed + $"{property.Name} = ");
                SerializeFields(value1, tabs);
            }
            else
            {
                Console.WriteLine(tabbed + $"{property.Name} = {value1},");
            }
        }

        FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
        foreach (FieldInfo field in fields)
        {
            object value1 = field.GetValue(exported);

            if (IsComplexType(field.FieldType))
            {
                Console.Write(tabbed + $"{field.Name} = ");
                SerializeFields(value1, tabs);
            }
            else
            {
                Console.WriteLine(tabbed + $"{field.Name} = {value1},");
            }
        }
        tabbed = new string('\t', --tabs);
        Console.WriteLine(tabbed + $"}}  // {type.Name}");
        Console.ResetColor();
    }

    public static string MoveTabLevel(int amount) 
    {
        return new string('\t', amount);
    }
}