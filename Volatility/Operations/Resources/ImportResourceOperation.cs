using System.Text.RegularExpressions;

using Volatility.Resources;
using Volatility.Utilities;

namespace Volatility.Operations.Resources;

internal partial class ImportResourceOperation
{
    private readonly string resourcesDirectory;
    private readonly string toolsDirectory;
    private readonly string splicerDirectory;
    private readonly bool overwrite;
    private static readonly object ConsolePromptLock = new();

    public ImportResourceOperation(string resourcesDirectory, string toolsDirectory, string splicerDirectory, bool overwrite)
    {
        this.resourcesDirectory = resourcesDirectory;
        this.toolsDirectory = toolsDirectory;
        this.splicerDirectory = splicerDirectory;
        this.overwrite = overwrite;
    }

    public async Task<ImportResourceResult> ExecuteAsync(ResourceType resourceType, Platform platform, string sourceFile, bool isX64)
    {
        Resource resource = ResourceFactory.CreateResource(resourceType, platform, sourceFile, isX64);

        string filePath = Path.Combine
        (
            resourcesDirectory,
            $"{DBToFileRegex().Replace(resource.AssetName, string.Empty)}.{resourceType}"
        );

        string? directoryPath = Path.GetDirectoryName(filePath);

        if (!string.IsNullOrEmpty(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        if (resourceType == ResourceType.Texture)
        {
            string texturePath = TextureBitmapUtilities.GetSecondaryBitmapPath(sourceFile, resource.Unpacker);

            if (resource is TextureBase texture && File.Exists(texturePath))
            {
                string outPath = Path.Combine
                (
                    directoryPath ?? string.Empty,
                    Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(Path.GetFullPath(filePath)))
                );

                TextureBitmapUtilities.WriteNormalizedBitmapFile(texture, texturePath, $"{outPath}.{resourceType}Bitmap", overwrite);
            }
        }

        if (resourceType == ResourceType.Splicer)
        {
            string sxPath = Path.Combine
            (
                toolsDirectory,
                "sx.exe"
            );

            bool sxExists = File.Exists(sxPath);

            Splicer? splicer = resource as Splicer;

            List<Splicer.SpliceSample>? samples = splicer?.GetLoadedSamples();

            string sampleDirectory = Path.Combine
            (
                splicerDirectory,
                "Samples"
            );

            Directory.CreateDirectory(sampleDirectory);

            if (samples != null)
            {
                for (int i = 0; i < samples.Count; i++)
                {
                    string sampleName = $"{samples[i].SampleID}";

                    string samplePathName = Path.Combine(sampleDirectory, sampleName);

                    if (!File.Exists($"{samplePathName}.snr") || overwrite)
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

                        if (!File.Exists(convertedSamplePathName) || overwrite)
                        {
                            Console.WriteLine($"Converting extracted sample {sampleName}.snr to wave...");
                            ProcessUtilities.RunAndRelayOutput(
                                sxPath,
                                $"-wave -s16l_int -v0 \"{samplePathName}.snr\" -=\"{convertedSamplePathName}\"");
                        }
                        else
                        {
                            Console.WriteLine($"Converted sample {Path.GetFileName(convertedSamplePathName)} already exists, skipping...");
                        }
                    }
                }
            }
        }

        return new ImportResourceResult(resource, filePath);
    }

    private static bool PromptOverwrite(string filePath)
    {
        lock (ConsolePromptLock)
        {
            Console.Write($"{Path.GetFileName(filePath)} already exists. Overwrite? [y/N]: ");
            string? input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input))
                return false;

            input = input.Trim();
            return input.Equals("y", StringComparison.OrdinalIgnoreCase)
                || input.Equals("yes", StringComparison.OrdinalIgnoreCase);
        }
    }

    [GeneratedRegex(@"(\?ID=\d+)|:")]
    private static partial Regex DBToFileRegex();
}

internal sealed record ImportResourceResult(Resource Resource, string ResourcePath);
