using System.Diagnostics;
using System.Text.RegularExpressions;

using Volatility.Resources;

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
            string texturePath = Path.Combine
            (
                Path.GetDirectoryName(sourceFile) ?? string.Empty,
                Path.GetFileNameWithoutExtension(sourceFile) +
                resource.Unpacker switch
                {
                    Unpacker.Bnd2Manager => "_2.bin",
                    Unpacker.DGI => "_texture.dat",
                    Unpacker.YAP => "_secondary.dat",
                    Unpacker.Raw => "_texture.dat",
                    Unpacker.Volatility => throw new NotImplementedException(),
                    _ => throw new NotImplementedException(),
                }
            );

            if (File.Exists(texturePath))
            {
                string outPath = Path.Combine
                (
                    directoryPath ?? string.Empty,
                    Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(Path.GetFullPath(filePath)))
                );

                File.Copy(texturePath, $"{outPath}.{resourceType}Bitmap", overwrite);
            }
        }

        if (resourceType == ResourceType.Shader && resource is ShaderPC shaderPc)
        {
            if (!string.IsNullOrEmpty(shaderPc.ShaderSourceText))
            {
                string shaderFileName = $"{Path.GetFileName(filePath)}.hlsl";
                string shaderPath = Path.Combine(directoryPath ?? string.Empty, shaderFileName);

                if (!File.Exists(shaderPath) || overwrite || PromptOverwrite(shaderPath))
                {
                    await File.WriteAllTextAsync(shaderPath, shaderPc.ShaderSourceText);
                }
                else
                {
                    Console.WriteLine($"Skipping extracted shader {Path.GetFileName(shaderPath)}.");
                }

                shaderPc.ShaderSourcePath = shaderFileName;
                shaderPc.ShaderSourceText = null;
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
                            ProcessStartInfo start = new ProcessStartInfo
                            {
                                FileName = sxPath,
                                Arguments = $"-wave -s16l_int -v0 \"{samplePathName}.snr\" -=\"{convertedSamplePathName}\"",
                                RedirectStandardOutput = true,
                                RedirectStandardError = true,
                                UseShellExecute = false,
                                CreateNoWindow = true
                            };

                            using Process process = new Process();
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
