using System.Text.RegularExpressions;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using Volatility.Resource.Splicer;
using Volatility.Resource.Texture;
using Volatility.Utilities;

namespace Volatility.CLI.Commands;

internal class ImportRawCommand : ICommand
{
	public string CommandToken => "ImportRaw";
	public string CommandDescription => "Imports information and relevant data from a specified platform's resoruce into a standardized format." +
		" NOTE: TUB format options are for the PC release of the title.";
	public string CommandParameters => "--recurse --overwrite --type=<resource type OR index> --format=<tub,bpr,x360,ps3> --path=<file path>";

	public string? RType { get; set; }
	public string? Format { get; set; }
	public string? ImportPath { get; set; }
	public bool Overwrite { get; set; }
	public bool Recursive { get; set; }

	public async Task Execute()
	{
		if (RType == "AUTO")
		{
			Console.WriteLine("Error: Automatic typing is not supported yet! Please specify a type (--type)");
			return;
		}
		
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

				var settings = new JsonSerializerSettings
				{
					Converters = new List<JsonConverter>
					{
						new ResourceJsonConverter(),
						new StringEnumConverter()
					},
					Formatting = Formatting.Indented
				};
				
				var serializedString = new string("");
				Resource.Resource resource = null;
				
				switch (RType)
				{
					case "TEXTURE":
						TextureHeaderBase? header = Format switch
						{
							"BPR" => new TextureHeaderBPR(sourceFile),
							"TUB" => new TextureHeaderPC(sourceFile),
							"X360" => new TextureHeaderX360(sourceFile),
							"PS3" => new TextureHeaderPS3(sourceFile),
							_ => throw new InvalidPlatformException(),
						};
						header.PullAll();
						serializedString = JsonConvert.SerializeObject(header, settings);
						resource = header;
						break;
					case "SPLICER":
						SplicerBase? splicer = Format switch
						{
							"BPR" => new SplicerLE(sourceFile),
							"TUB" => new SplicerLE(sourceFile),
							"X360" => new SplicerBE(sourceFile),
							"PS3" => new SplicerBE(sourceFile),
							_ => throw new InvalidPlatformException(),
						};
						serializedString = JsonConvert.SerializeObject(splicer, settings);
						resource = splicer;
						break;
				}


				var ResourceClass = resource.GetType();
				var ResourceType = ResourceClass.GetProperty("ResourceType", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);

				
				string dataPath = Path.Combine
				(
					Directory.GetCurrentDirectory(),
					"data",
					"Resources"
				);

				string filePath = Path.Combine(dataPath, $"{Regex.Replace(resource.AssetName, @"(\?ID=\d+)|:", "")}.json");

				string directoryPath = Path.GetDirectoryName(filePath);

				Directory.CreateDirectory(directoryPath);

				using (StreamWriter streamWriter = new StreamWriter(filePath))
				{
					await streamWriter.WriteAsync(serializedString);
				};

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
				
				Console.WriteLine($"Imported {Path.GetFileName(ImportPath)} as {Path.GetFullPath(filePath)}.");
			}));
		}

		await Task.WhenAll(tasks);
	}
	public void SetArgs(Dictionary<string, object> args)
	{
		RType = (args.TryGetValue("type", out object? rtype) ? rtype as string : "auto").ToUpper();
		Format = (args.TryGetValue("format", out object? format) ? format as string : "auto").ToUpper();
		ImportPath = args.TryGetValue("path", out object? path) ? path as string : "";
		Overwrite = args.TryGetValue("overwrite", out var ow) && (bool)ow;
		Recursive = args.TryGetValue("recurse", out var re) && (bool)re;
	}
}