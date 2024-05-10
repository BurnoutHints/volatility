using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using Volatility.TextureHeader;

using Volatility.Utilities;

namespace Volatility.CLI.Commands;

internal class ImportRawCommand : ICommand
{
    public string CommandToken => "ImportRaw";
    public string CommandDescription => "NOTE: TUB & BPR format options are for the PC releases of the title.";
    public string CommandParameters => "--format=<tub,bpr,x360,ps3> --path=<file path>";

    public string? Format { get; set; }
    public string? ImportPath { get; set; }

    public void Execute()
    {
        if (string.IsNullOrEmpty(ImportPath))
        {
            Console.WriteLine("Error: No import path specified! (--path)");
            return;
        }

        foreach (string sourceFile in ICommand.GetFilesInDirectory(ImportPath))
        {
            FileAttributes fileAttributes;
            try
            {
                fileAttributes = File.GetAttributes(ImportPath);
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
                "BPR" => new TextureHeaderBPR(ImportPath),
                "TUB" => new TextureHeaderPC(ImportPath),
                "X360" => new TextureHeaderX360(ImportPath),
                "PS3" => new TextureHeaderPS3(ImportPath),
                _ => throw new InvalidPlatformException(),
            };

            header.PullAll();

            var settings = new JsonSerializerSettings
            {
                Converters = new List<JsonConverter>
                {
                    new TextureHeaderJsonConverter(),
                    new StringEnumConverter()
                },
                Formatting = Formatting.Indented
            };
            var serializedString = JsonConvert.SerializeObject(header, settings);

            string directoryPath = Path.Combine
            (
                Directory.GetCurrentDirectory(),
                "data",
                Directory.GetParent(header.ImportPath).Parent.Name,
                Directory.GetParent(header.ImportPath).Name
            );

            string filePath = Path.Combine(directoryPath, $"{header.AssetName}.json");

            Directory.CreateDirectory(directoryPath);

            using (StreamWriter streamWriter = new(filePath))
            {
                streamWriter.Write(serializedString);
            };
            
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine(serializedString);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Imported {ImportPath}.");
            Console.ResetColor();
        }
    }
    public void SetArgs(Dictionary<string, object> args)
    {
        Format = (args.TryGetValue("format", out object? format) ? format as string : "auto").ToUpper();
        ImportPath = args.TryGetValue("path", out object? path) ? path as string : "";
    }
}