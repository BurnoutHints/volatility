using System.Runtime.Serialization;

using Volatility.Resources;
using Volatility.Resources.Splicer;
using Volatility.Resources.Texture;
using Volatility.Utilities;

namespace Volatility.CLI.Commands;

internal partial class ExportResourceCommand : ICommand
{
	public static string CommandToken => "ExportResource";
	public static string CommandDescription => "Exports information and relevant data from an imported/created resource into a platform's format.";
	public static string CommandParameters => "--recurse --overwrite --type=<resource type OR index> --format=<tub,bpr,x360,ps3> --respath=<file path>";

	public string? Format { get; set; }
	public string? ResourcePath { get; set; }
	public string? OutputPath { get; set; }
	public bool Overwrite { get; set; }
	public bool Recursive { get; set; }

	public async Task Execute()
	{
        if (string.IsNullOrEmpty(Format))
        {
            Console.WriteLine("Error: No resource path specified! (--respath)");
            return;
        }
        if (string.IsNullOrEmpty(ResourcePath))
		{
			Console.WriteLine("Error: No resource path specified! (--respath)");
			return;
		}
		if (string.IsNullOrEmpty(OutputPath))
		{
			Console.WriteLine("Error: No output path specified! (--outpath)");
			return;
		}

        string filePath = $"{Path.Combine("data", "Resources", ResourcePath)}";

        string[] sourceFiles = ICommand.GetFilePathsInDirectory(filePath, ICommand.TargetFileType.Any, Recursive);

		if (sourceFiles.Length == 0)
		{
			Console.WriteLine($"Error: No valid file(s) found at the specified path ({ResourcePath}). Ensure the path exists and spaces are properly enclosed. (--path)");
			return;
		}

		List<Task> tasks = new List<Task>();
		foreach (string sourceFile in sourceFiles)
		{
            Console.WriteLine(sourceFile);

            tasks.Add(Task.Run(async () =>
			{
				FileAttributes fileAttributes;
				try
				{
					fileAttributes = File.GetAttributes(sourceFile);
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
					Console.WriteLine($"Error: Caught file exception: {e.Message}");
					return;
				}

                ResourceType resourceType = ResourceType.Invalid;
                Enum.TryParse(Path.GetExtension(filePath).TrimStart('.'), true, out resourceType);

				Type resourceClass;

                // This method is most definitely temporary.
                switch (resourceType)
				{
					case ResourceType.Splicer:
						resourceClass = Format switch
                        {
                            "BPR" => typeof(SplicerLE),
                            "TUB" => typeof(SplicerLE),
                            "PS3" => typeof(SplicerBE),
                            "X360" => typeof(SplicerBE),
                            _ => throw new InvalidPlatformException(),
                        };
						break;
					case ResourceType.Texture:
                        resourceClass = Format switch
                        {
                            "BPR" => typeof(TextureHeaderBPR),
                            "TUB" => typeof(TextureHeaderPC),
                            "PS3" => typeof(TextureHeaderPS3),
                            "X360" => typeof(TextureHeaderX360),
                            _ => throw new InvalidPlatformException(),
                        };
						break;
                    default:
                        Console.WriteLine($"ERROR: Exporting {resourceType} to {Format} is not supported!");
                        return;
				}
				string json = File.ReadAllText(sourceFile);

                int startIndex = json.IndexOf('{');
                if (startIndex != -1)
                {
                    json = json.Substring(startIndex);
                }

                Resource? resource = null;
				try
				{

                    resource = (Resource?)ResourceJsonConverter.DeserializeResource(resourceClass, json);
                    if (resource is not Resource)
					{
                        throw new SerializationException();
                    }
                }
				catch (Exception e)
				{
                    Console.WriteLine($"ERROR: Unable to deserialize {Path.GetFileName(sourceFile)} as {resourceType}!\nMessage from {e.TargetSite}: {e.Message}.\nStack Trace:\n{e.StackTrace}");
                }

				Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));

                using (FileStream fs = new(OutputPath, FileMode.Create))
                {
					using (EndianAwareBinaryWriter writer = new(fs, resource.GetResourceEndian()))
					{
						// The way this is handled is pending a pipeline rewrite
                        resource.WriteToStream(writer);
						if (resourceType == ResourceType.Splicer)
						{
							(resource as SplicerBase).SpliceSamples(writer, Path.GetDirectoryName(sourceFile));
                        }
						else if (resourceType == ResourceType.Texture)
						{
							// TODO: Export bitmap data
						}
					}
                }

                Console.WriteLine($"Exported {Path.GetFileName(ResourcePath)} as {Path.GetFullPath(sourceFile)}.");
			}));
		}
		await Task.WhenAll(tasks);
	}

    public void SetArgs(Dictionary<string, object> args)
	{
		Format = (args.TryGetValue("format", out object? format) ? format as string : "")?.ToUpper();
		ResourcePath = args.TryGetValue("respath", out object? respath) ? respath as string : "";
		OutputPath = args.TryGetValue("outpath", out object? outpath) ? outpath as string : "";
		Overwrite = args.TryGetValue("overwrite", out var ow) && (bool)ow;
		Recursive = args.TryGetValue("recurse", out var re) && (bool)re;
	}
}