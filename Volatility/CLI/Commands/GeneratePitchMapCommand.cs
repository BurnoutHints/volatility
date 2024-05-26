using Newtonsoft.Json;
using System.Text;
using Volatility.TextureHeader;

namespace Volatility.CLI.Commands;

internal class GeneratePitchMapCommand : ICommand
{
    public string CommandToken => "GeneratePitchMap";
    public string CommandDescription => "Generates a map of pitch values from X360 texture headers.";
    public string CommandParameters => "[--recurse] [--overwrite] --path=<file path>";

    public string? ImportPath { get; set; }
    public bool Overwrite { get; set; }
    public bool Recursive { get; set; }

    public async Task Execute()
    {
        if (string.IsNullOrEmpty(ImportPath))
        {
            Console.WriteLine("Error: No import path specified! (--path)");
            return;
        }

        string[] sourceFiles = ICommand.GetFilePathsInDirectory(ImportPath, ICommand.TargetFileType.TextureHeader, Recursive);
        List<Task> tasks = new List<Task>();

        Dictionary<(int, int), (int, int)> outPitches = new Dictionary<(int, int), (int, int)> { };

        foreach (string sourceFile in sourceFiles)
        {
            tasks.Add(Task.Run(async () =>
            {
                using (FileStream fs = new FileStream(sourceFile, FileMode.Open))
                {
                    using (BinaryReader reader = new BinaryReader(fs))
                    {
                        // Only checking based on stream length right now
                        if (fs.Length > 0x40 || fs.Length < 0x34)
                        {
                            Console.WriteLine($"Provided asset {Path.GetFileNameWithoutExtension(sourceFile)} is not a X360 texture header! Skipping.");
                            reader.Close();
                            fs.Close();
                            return;
                        }
                        reader.Close();
                    }
                    fs.Close();
                }

                var header = new TextureHeaderX360(sourceFile);

                Dictionary<(int Width, int Height), (int, int)> outPitch = new Dictionary<(int, int), (int, int)>
                {
                    {((int)header.Format.Size.Width, (int)header.Format.Size.Height), (header.Format.Pitch, (int)header.Format.MipAddress)}
                };

                if (Overwrite || !outPitches.ContainsKey(outPitch.First().Key))
                {
                    outPitches.TryAdd(outPitch.First().Key, outPitch.First().Value);
                }
            }));
        }

        await Task.WhenAll(tasks);

        string directoryPath = Path.Combine(Directory.GetCurrentDirectory(), "data");
        Directory.CreateDirectory(directoryPath);
        string csonFileName = Path.Combine(directoryPath, "_pitchmap.cson");
        StringBuilder cson = new();

        foreach (var group in outPitches)
        {
            cson.AppendLine($"{{ {group.Key},   {group.Value} }},");
        }

        await File.WriteAllTextAsync(csonFileName, cson.ToString());
        Console.WriteLine($"Pitch map written to file.");
    }

    public void SetArgs(Dictionary<string, object> args)
    {
        ImportPath = args.TryGetValue("path", out object? path) ? path as string : "";
        Overwrite = args.TryGetValue("overwrite", out var ow) && (bool)ow;
        Recursive = args.TryGetValue("recurse", out var re) && (bool)re;
    }
}
