using System.Text.RegularExpressions;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using Volatility.Resource.Texture;
using Volatility.Utilities;

namespace Volatility.CLI.Commands;

internal class ImportRawCommand : ICommand
{
	public string CommandToken => "ImportRaw";
	public string CommandDescription => "Imports information and relevant data from a specified platform's resoruce into a standardized format." +
		" NOTE: TUB format options are for the PC release of the title.";
	public string CommandParameters => "--recurse --overwrite --type=<resource type OR index> --format=<tub,bpr,x360,ps3> --path=<file path>";

	public string? Format { get; set; }
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

		var sourceFiles = ICommand.GetFilePathsInDirectory(ImportPath, ICommand.TargetFileType.Header, Recursive);
		List<Task> tasks = new List<Task>();

		foreach (string sourceFile in ICommand.GetFilePathsInDirectory(ImportPath, ICommand.TargetFileType.Header, Recursive))
		{
			tasks.Add(Task.Run(async () =>
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
					Console.WriteLine($"Error: Caught file exception: {e.Message}");
					return;
				}

				Console.WriteLine($"Constructing {Format} resource property data...");

				TextureHeaderBase? header = Format switch
				{
					"BPR" => new TextureHeaderBPR(sourceFile),
					"TUB" => new TextureHeaderPC(sourceFile),
					"X360" => new TextureHeaderX360(sourceFile),
					"PS3" => new TextureHeaderPS3(sourceFile),
					_ => throw new InvalidPlatformException(),
				};

				// ENDHELP 1

				header.PullAll();

				var ResourceClass = header.GetType();
				var ResourceType = ResourceClass.GetProperty("ResourceType", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);

				var settings = new JsonSerializerSettings
				{
					Converters = new List<JsonConverter>
				{
					new ResourceJsonConverter(),
					new StringEnumConverter()
				},
					Formatting = Formatting.Indented
				};
				var serializedString = JsonConvert.SerializeObject(header, settings);

				string dataPath = Path.Combine
				(
					Directory.GetCurrentDirectory(),
					"data",
					"Resources"
				);

				string filePath = Path.Combine(dataPath, $"{Regex.Replace(header.AssetName, @"(\?ID=\d+)|:", "")}.json");

				string directoryPath = Path.GetDirectoryName(filePath);

				Directory.CreateDirectory(directoryPath);

				using (StreamWriter streamWriter = new StreamWriter(filePath))
				{
					await streamWriter.WriteAsync(serializedString);
				};

				// HELP 2: This is the step that handles external resource data. Right now, it's hardcoded to handle what textures need, but this may also need to be another function that takes a string on a resource type. the problem is that there may be more than one resource type that has the same external resoruce type, there's also different formats to consider, and different resources may have the same external resource data's name but be for a different purpose.
				string texturePath = Path.Combine
				(
					Path.GetDirectoryName(sourceFile),
					Path.GetFileNameWithoutExtension(sourceFile) + "_texture.dat"
				);

				if (File.Exists(texturePath))
				{
					string outPath = Path.Combine
					(
						directoryPath,
						Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(Path.GetFullPath(filePath)))
					);

					File.Copy(texturePath, $"{outPath}.{ResourceType}", Overwrite);
				}

				// Console.ForegroundColor = ConsoleColor.DarkCyan;
				// Console.WriteLine(serializedString);

				// Console.ForegroundColor = ConsoleColor.Green;
				Console.WriteLine($"Imported {Path.GetFileName(ImportPath)} as {Path.GetFullPath(filePath)}.");
				// Console.ResetColor();
			}));
		}

		await Task.WhenAll(tasks);
	}
	public void SetArgs(Dictionary<string, object> args)
	{
		Format = (args.TryGetValue("format", out object? format) ? format as string : "auto").ToUpper();
		ImportPath = args.TryGetValue("path", out object? path) ? path as string : "";
		Overwrite = args.TryGetValue("overwrite", out var ow) && (bool)ow;
		Recursive = args.TryGetValue("recurse", out var re) && (bool)re;
	}
}