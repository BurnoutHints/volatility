using System.Diagnostics;
using System.Text.RegularExpressions;

using YamlDotNet.Serialization;

using Volatility.Resources;
using Volatility.Utilities;

using static Volatility.Utilities.EnvironmentUtilities;

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

				var serializer = new SerializerBuilder()
    				.DisableAliases()
    				.WithTypeInspector(inner => new IncludeFieldsTypeInspector(inner))
                    .WithTypeConverter(new ResourceYamlTypeConverter())
                    .WithTypeConverter(new StrongIDYamlTypeConverter())
                    .WithTypeConverter(new StringEnumYamlTypeConverter())
    				.Build();
				
				var serializedString = new string("");

    			bool isX64 = Format.EndsWith("x64", StringComparison.OrdinalIgnoreCase);
    			if (isX64)
    			    Format = Format[..^3];

                if (!DataUtilities.TryParseEnum(Format, out Platform platform)) 
				{
                    throw new InvalidPlatformException("Error: Invalid file format specified!");
                }

                if (!DataUtilities.TryParseEnum(ResType, out ResourceType resType)) 
				{
                    Console.WriteLine("Error: Invalid resource type specified!");
                    return;
                }

                Resource resource = ResourceFactory.CreateResource(resType, platform, sourceFile, isX64);

				var resourceClass = resource.GetType();
				var resourceType = resource.GetResourceType();

				string filePath = Path.Combine
				(
					GetEnvironmentDirectory(EnvironmentDirectory.Resources), 
					$"{DBToFileRegex().Replace(resource.AssetName, "")}.{resourceType}"
				);

				string? directoryPath = Path.GetDirectoryName(filePath);

				Directory.CreateDirectory(directoryPath);

				serializedString = serializer.Serialize(resource);
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
                    string sxPath = Path.Combine
                    (
                        GetEnvironmentDirectory(EnvironmentDirectory.Tools),
                        "sx.exe"
                    );

                    bool sxExists = File.Exists(sxPath);

                    Splicer? splicer = resource as Splicer;

					List<Splicer.Sample>? samples = splicer?.GetLoadedSamples();
                    for (int i = 0; i < samples?.Count; i++)
                    {
                        string sampleDirectory = Path.Combine
                        (
                            GetEnvironmentDirectory(EnvironmentDirectory.Splicer),
                            "Samples"
                        );

                        Directory.CreateDirectory(sampleDirectory);

                        string sampleName = $"{samples[i].SampleID}";

						string samplePathName = Path.Combine(sampleDirectory, sampleName);

                        if (!File.Exists($"{samplePathName}.snr") || Overwrite)
						{
                            Console.WriteLine($"Writing extracted sample {sampleName}.snr");
                            await File.WriteAllBytesAsync($"{samplePathName}.snr", samples[i].Data);
                        }
						else
						{
                            Console.WriteLine($"Skipping extracted sample {sampleName}.snr");
                        }

                        if (sxExists)
						{
							string convertedSamplePathName = Path.Combine(sampleDirectory, "_extracted");

							Directory.CreateDirectory(convertedSamplePathName);

							convertedSamplePathName = Path.Combine(convertedSamplePathName, sampleName + ".wav");

							if (!File.Exists(convertedSamplePathName) || Overwrite)
							{
                                ProcessStartInfo start = new ProcessStartInfo
                                {
                                    FileName = sxPath,
                                    Arguments = $"-wave -s16l_int -v0 \"{samplePathName}.snr\" -=\"{convertedSamplePathName}\"",
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
							else
							{
                                Console.WriteLine($"Converted sample {Path.GetFileName(convertedSamplePathName)} already exists, skipping...");
                            }
                        }
                    }
                }
                Console.WriteLine($"Imported {Path.GetFileName(sourceFile)} as {Path.GetFullPath(filePath)}.");
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