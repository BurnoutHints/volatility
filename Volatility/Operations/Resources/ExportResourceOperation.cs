using Volatility.Resources;
using Volatility.Utilities;

namespace Volatility.Operations.Resources;

internal class ExportResourceOperation
{
    public Task ExecuteAsync(Resource resource, string outputPath, Platform platform)
    {
        string? directoryPath = Path.GetDirectoryName(outputPath);

        if (!string.IsNullOrEmpty(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        using FileStream fs = new(outputPath, FileMode.Create);

        Endian endian = resource.GetResourceEndian() != Endian.Agnostic
            ? resource.GetResourceEndian()
            : EndianMapping.GetDefaultEndian(platform);

        using EndianAwareBinaryWriter writer = new(fs, endian);

        switch (resource)
        {
            case TextureBase texture:
                texture.PushAll();
                goto default;
            default:
                resource.WriteToStream(writer);
                break;
        }

        if (resource is ShaderBase shader)
        {
            var stages = shader.GetCompileStages();
            bool useStageSuffix = stages.Count > 1;

            if (platform == Platform.BPR)
            {
                var bufferOperation = new CreateShaderProgramBufferOperation();
                foreach (var stage in stages)
                {
                    string shaderProgramBufferPath = GetShaderProgramBufferPath(outputPath, stage, useStageSuffix);
                    string csoPath = GetShaderCSOPath(outputPath, stage, useStageSuffix);

                    DxcShaderCompiler.CompileToCSO(shader, stage, csoPath);
                    ShaderProgramBufferBase buffer = bufferOperation.ExecuteFromFile(csoPath, stage.ResolveStage(), platform);
                    bufferOperation.WriteToFile(buffer, shaderProgramBufferPath, platform);
                    WritePaddedCSOFile(csoPath, GetSecondaryResourcePath(shaderProgramBufferPath));
                }
            }
            else
            {
                DxcShaderCompiler.CompileStagesToCSO(shader, stages, stage =>
                    GetShaderProgramBufferPath(outputPath, stage, useStageSuffix));
            }
        }

        if (resource is ShaderProgramBufferBPR shaderProgramBuffer && platform == Platform.BPR)
        {
            if (shaderProgramBuffer.CompiledShaderBytecode.Length > 0)
            {
                string secondaryCsoPath = GetSecondaryResourcePath(outputPath);
                WritePaddedCSOBytes(shaderProgramBuffer.CompiledShaderBytecode, secondaryCsoPath);
            }
        }

        return Task.CompletedTask;
    }

    private static string GetShaderProgramBufferPath(
        string shaderOutputPath,
        ShaderStageCompile stage,
        bool useStageSuffix)
    {
        string? directory = Path.GetDirectoryName(shaderOutputPath);
        string baseName = Path.GetFileNameWithoutExtension(shaderOutputPath);
        string stageSuffix = useStageSuffix ? $".{ShaderStageCompile.GetStageSuffix(stage.ResolveStage())}" : string.Empty;
        string fileName = $"{baseName}.secondary{stageSuffix}.ShaderProgramBuffer";
        return string.IsNullOrEmpty(directory)
            ? fileName
            : Path.Combine(directory, fileName);
    }

    private static string GetShaderCSOPath(
        string shaderOutputPath,
        ShaderStageCompile stage,
        bool useStageSuffix)
    {
        string? directory = Path.GetDirectoryName(shaderOutputPath);
        string baseName = Path.GetFileNameWithoutExtension(shaderOutputPath);
        string stageSuffix = useStageSuffix ? $".{ShaderStageCompile.GetStageSuffix(stage.ResolveStage())}" : string.Empty;
        string fileName = $"{baseName}.secondary{stageSuffix}.cso";
        return string.IsNullOrEmpty(directory)
            ? fileName
            : Path.Combine(directory, fileName);
    }

    private static string GetSecondaryResourcePath(string primaryPath)
    {
        string? directory = Path.GetDirectoryName(primaryPath);
        string baseName = Path.GetFileNameWithoutExtension(primaryPath);
        string extension = Path.GetExtension(primaryPath);
        string fileName = $"{baseName}.secondary{extension}";
        return string.IsNullOrEmpty(directory)
            ? fileName
            : Path.Combine(directory, fileName);
    }

    private static void WritePaddedCSOFile(string sourcePath, string outputPath)
    {
        string? directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using FileStream input = new(sourcePath, FileMode.Open, FileAccess.Read);
        using FileStream output = new(outputPath, FileMode.Create, FileAccess.Write);
        input.CopyTo(output);

        PaddingUtilities.WritePadding(output, 0x100);
    }

    private static void WritePaddedCSOBytes(byte[] csoBytes, string outputPath)
    {
        string? directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using FileStream output = new(outputPath, FileMode.Create, FileAccess.Write);
        output.Write(csoBytes);

        PaddingUtilities.WritePadding(output, 0x100);
    }
}
