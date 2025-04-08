using System.Diagnostics;
using System.Text.RegularExpressions;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using Volatility.Resources;
using Volatility.Utilities;

namespace Volatility.CLI.Commands;

internal partial class ImportResourceCommand : ICommand
{
	public static string CommandToken => "ImportResource";
	public static string CommandDescription => "Imports information and relevant data from a specified platform's resource into a standardized format.";
	public static string CommandParameters => "--recurse --overwrite --type=<resource type OR index> --format=<tub,bpr,x360,ps3> --path=<file path>";

	public string? ResType { get; set; }
	public string? Format { get; set; }
	public string? ImportPath { get; set; }
	public bool Overwrite { get; set; }
	public bool Recursive { get; set; }

	public async Task Execute()
	{
		if (ResType == "AUTO")
		{
			Console.WriteLine("Error: Automatic typing is not supported yet! Please specify a type (--type)");
			return;
		}
		
		if (string.IsNullOrEmpty(ImportPath))
		{
			Console.WriteLine("Error: No import path specified! (--path)");
			return;
		}

		string[] sourceFiles = ICommand.GetFilePathsInDirectory(ImportPath, ICommand.TargetFileType.Header, Recursive);

		if (sourceFiles.Length == 0)
		{
			Console.WriteLine($"Error: No valid file(s) found at the specified path ({ImportPath}). Ensure the path exists and spaces are properly enclosed. (--path)");
			return;
		}

		List<Task> tasks = new List<Task>();
		foreach (string sourceFile in sourceFiles)
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

                if (!DataUtilities.TryParseEnum(Format, out Platform platform)) 
				{
                    throw new InvalidPlatformException("Error: Invalid file format specified!");
                }

                if (!DataUtilities.TryParseEnum(ResType, out ResourceType resType)) 
				{
                    Console.WriteLine("Error: Invalid resource type specified!");
                    return;
                }

                Resource resource = ResourceFactory.CreateResource(resType, platform, sourceFile);

				var resourceClass = resource.GetType();
				var resourceType = resource.GetResourceType();
				
				string dataPath = Path.Combine
				(
					Directory.GetCurrentDirectory(),
					"data",
					"Resources"
				);

				string filePath = Path.Combine(dataPath, $"{DBToFileRegex().Replace(resource.AssetName, "")}.{resourceType}");

				string? directoryPath = Path.GetDirectoryName(filePath);

				Directory.CreateDirectory(directoryPath);

				serializedString = JsonConvert.SerializeObject(resource, settings);
				using (StreamWriter streamWriter = new StreamWriter(filePath))
				{
					await streamWriter.WriteAsync(serializedString);
				};

				// Texture-specific logic. Will need to refactor this pipeline
				if (resourceType == ResourceType.Texture)
				{
                    string texturePath = Path.Combine
					(
						Path.GetDirectoryName(sourceFile),
						Path.GetFileNameWithoutExtension(sourceFile) +
                        // TODO: Resource-defined Secondary path support
                        resource.Unpacker switch
                        {
                            Unpacker.Bnd2Manager => "_2.bin",
                            Unpacker.DGI => "_texture.dat",
                            Unpacker.YAP => "_secondary.dat",
                            Unpacker.Raw => "_texture.dat", // Fallback for now
                            Unpacker.Volatility => throw new NotImplementedException(),
                            _ => throw new NotImplementedException(),
                        }
                    );

					if (File.Exists(texturePath))
					{
						string outPath = Path.Combine
						(
							directoryPath,
							Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(Path.GetFullPath(filePath)))
						);

						File.Copy(texturePath, $"{outPath}.{resourceType}Bitmap", Overwrite);
					}
				}

                // Splicer-specific logic. Will need to refactor this pipeline
                if (resourceType == ResourceType.Splicer)
                {
                    string sxPath = Path.Combine("tools", $"sx.exe");
					bool sxExists = File.Exists(sxPath);

                    Splicer? splicer = resource as Splicer;

					byte[][]? samples = splicer?.GetLoadedSamples();

                    for (int i = 0; i < samples?.Length; i++)
					{
						string sampleName = $"{resource.AssetName}_{i}";

						string sampleDirectory = Path.Combine(directoryPath, $"{resource.AssetName}_Samples");

                        Directory.CreateDirectory(sampleDirectory);

						Console.WriteLine($"Writing extracted sample {sampleName}.snr");
					    await File.WriteAllBytesAsync(Path.Combine(sampleDirectory, $"{sampleName}.snr"), samples[i]);

						if (sxExists)
						{
							string samplePathName = Path.Combine(sampleDirectory, sampleName);

							string convertedSamplePathName = Path.Combine(sampleDirectory, "_extracted");

							Directory.CreateDirectory(convertedSamplePathName);

							convertedSamplePathName = Path.Combine(convertedSamplePathName, sampleName);

                            ProcessStartInfo start = new ProcessStartInfo
					        {
					            FileName = sxPath,
					            Arguments = $"-wave -s16l_int -v0 \"{samplePathName}.snr\" -=\"{convertedSamplePathName}.wav\"",
					            RedirectStandardOutput = true,
					            RedirectStandardError = true,
					            UseShellExecute = false,
					            CreateNoWindow = true
					        };

					        using (Process process = new Process())
					        {
					            process.StartInfo = start;
					            process.OutputDataReceived += (sender, e) =>
					            {
					                if (!string.IsNullOrEmpty(e.Data)) Console.WriteLine(e.Data);
					            };

					            process.ErrorDataReceived += (sender, e) =>
					            {
					                if (!string.IsNullOrEmpty(e.Data)) Console.WriteLine(e.Data);
					            };

                                Console.WriteLine($"Converting extracted sample {sampleName}.snr to wave...");
                                process.Start();
					            process.BeginOutputReadLine();
					            process.BeginErrorReadLine();
					            process.WaitForExit();
					        }
					    }
					}
				}
                Console.WriteLine($"Imported {Path.GetFileName(ImportPath)} as {Path.GetFullPath(filePath)}.");
			}));
		}
		await Task.WhenAll(tasks);
	}
	
	[GeneratedRegex(@"(\?ID=\d+)|:")]
	private static partial Regex DBToFileRegex();
	
	public void SetArgs(Dictionary<string, object> args)
	{
		ResType = (args.TryGetValue("type", out object? restype) ? restype as string : "auto")?.ToUpper();
		Format = (args.TryGetValue("format", out object? format) ? format as string : "auto")?.ToUpper();
		ImportPath = args.TryGetValue("path", out object? path) ? path as string : "";
		Overwrite = args.TryGetValue("overwrite", out var ow) && (bool)ow;
		Recursive = args.TryGetValue("recurse", out var re) && (bool)re;
	}
    public ImportResourceCommand() { }
}