using System.Text.RegularExpressions;
using Volatility.Abstractions.Services;
using Volatility.Resources;

namespace Volatility.Operations.Resources;

internal sealed partial class ImportResourceOperation(
    IResourceDBLookup resourceDBLookup,
    ITextureBitmapStore textureBitmapStore,
    IProcessRunner processRunner,
    IShaderSourceStore shaderSourceStore)
{
    public async Task<ImportResourceResult> ExecuteAsync(ImportResourceRequest request)
    {
        Resource resource = ResourceFactory.LoadResource(
            request.ResourceType,
            request.Platform,
            request.SourceFile,
            resourceDBLookup,
            request.IsX64);

        string filePath = Path.Combine
        (
            request.ResourcesDirectory,
            $"{DBToFileRegex().Replace(resource.AssetName, string.Empty)}.{request.ResourceType}"
        );

        string? directoryPath = Path.GetDirectoryName(filePath);

        if (!string.IsNullOrEmpty(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        if (resource is ShaderBase shader)
        {
            shaderSourceStore.MaterializeImportedSource(shader, request.ResourcesDirectory);
        }

        if (request.ResourceType == ResourceType.Texture)
        {
            string texturePath = textureBitmapStore.GetSecondaryBitmapPath(request.SourceFile, resource.Unpacker);

            if (resource is TextureBase texture && File.Exists(texturePath))
            {
                string outPath = Path.Combine
                (
                    directoryPath ?? string.Empty,
                    Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(Path.GetFullPath(filePath)))
                );

                textureBitmapStore.WriteNormalizedBitmapFile(texture, texturePath, $"{outPath}.{request.ResourceType}Bitmap", request.Overwrite);
            }
        }

        if (request.ResourceType == ResourceType.Splicer)
        {
            string sxPath = Path.Combine
            (
                request.ToolsDirectory,
                "sx.exe"
            );

            bool sxExists = File.Exists(sxPath);

            Splicer? splicer = resource as Splicer;

            List<Splicer.SpliceSample>? samples = splicer?.GetLoadedSamples();

            string sampleDirectory = Path.Combine
            (
                request.SplicerDirectory,
                "Samples"
            );

            Directory.CreateDirectory(sampleDirectory);

            if (samples != null)
            {
                for (int i = 0; i < samples.Count; i++)
                {
                    string sampleName = $"{samples[i].SampleID}";

                    string samplePathName = Path.Combine(sampleDirectory, sampleName);

                    if (!File.Exists($"{samplePathName}.snr") || request.Overwrite)
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

                        if (!File.Exists(convertedSamplePathName) || request.Overwrite)
                        {
                            Console.WriteLine($"Converting extracted sample {sampleName}.snr to wave...");
                            processRunner.RunAndRelayOutput(
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

    [GeneratedRegex(@"(\?ID=\d+)|:")]
    private static partial Regex DBToFileRegex();
}

internal sealed record ImportResourceRequest(
    ResourceType ResourceType,
    Platform Platform,
    string SourceFile,
    bool IsX64,
    string ResourcesDirectory,
    string ToolsDirectory,
    string SplicerDirectory,
    bool Overwrite);

internal sealed record ImportResourceResult(Resource Resource, string ResourcePath);
