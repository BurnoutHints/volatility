using System.Runtime.Serialization;

using Volatility.Resources;
using Volatility.Utilities;

using static Volatility.Utilities.EnvironmentUtilities;

namespace Volatility.CLI.Commands;

internal partial class ExportResourceCommand : ICommand
{
	public static string CommandToken => "ExportResource";
	public static string CommandDescription => "Exports information and relevant data from an imported/created resource into a platform's format.";
	public static string CommandParameters => "--recurse --overwrite --type=<resource type OR index> --format=<tub,bpr,x360,ps3> --respath=<data path> --outpath=<file path>";

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

        string filePath = $"" +
			$"{	Path.Combine
				(
					GetEnvironmentDirectory(EnvironmentDirectory.Resources), 
					ResourcePath
				)
			}";

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

                if (!DataUtilities.TryParseEnum(Format, out Platform platform))
                {
                    throw new InvalidPlatformException("Error: Invalid file format specified!");
                }

                if (!DataUtilities.TryParseEnum(Path.GetExtension(filePath).TrimStart('.'), out ResourceType resourceType))
                {
                    Console.WriteLine("Error: Resource type is invalid!");
                    return;
                }

                string yaml = File.ReadAllText(sourceFile);

                Resource resource = ResourceFactory.CreateResource(resourceType, platform, "");
				try
				{
                    resource = (Resource?)ResourceYamlDeserializer.DeserializeResource(resource.GetType(), yaml);
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
					Endian endian = resource.GetResourceEndian() != Endian.Agnostic 
						? resource.GetResourceEndian() 
						: EndianMapping.GetDefaultEndian(platform);

					using (EndianAwareBinaryWriter writer = new(fs, endian))
					{
                        // The way this is handled is pending a pipeline rewrite
						switch (resource)
						{
							case TextureBase texture:
                                texture.PushAll();
                                goto default;
							default:
								resource.WriteToStream(writer);
								break;
                        }
					}
                }

                Console.WriteLine($"Exported {Path.GetFileName(ResourcePath)} as {Path.GetFullPath(OutputPath)}.");
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

	public ExportResourceCommand() { }
}