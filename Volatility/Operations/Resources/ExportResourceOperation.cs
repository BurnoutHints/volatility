using Volatility.Resources;
using Volatility.Utilities;
namespace Volatility.Operations.Resources;

internal class ExportResourceOperation
{
    public Task ExecuteAsync(
        Resource resource,
        string outputPath,
        Platform platform,
        Unpacker? importUnpackerOverride = null,
        bool writeImportsToSeparateFile = false)
    {
        string? directoryPath = Path.GetDirectoryName(outputPath);

        if (!string.IsNullOrEmpty(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        using FileStream fs = new(outputPath, FileMode.Create);

        Endian endian = resource.ResourceEndian != Endian.Agnostic
            ? resource.ResourceEndian
            : EndianMapping.GetDefaultEndian(platform);

        using ResourceBinaryWriter writer = new(fs, endian);

        switch (resource)
        {
            case TextureBase texture:
                texture.PushAll();
                goto default;
            default:
                resource.WriteToStream(writer);
                break;
        }

        WriteExternalImports(
            resource,
            outputPath,
            writer,
            endian,
            ResolveExternalImportsUnpackerFormat(resource, importUnpackerOverride),
            writeImportsToSeparateFile);

        if (resource is ShaderBase shader)
        {
            var stages = shader.GetCompileStages();
            bool useStageSuffix = stages.Count > 1;

            if (platform == Platform.BPR)
            {
                CreateShaderProgramBufferOperation bufferOperation = new();
                foreach (var stage in stages)
                {
                    string shaderProgramBufferPath = GetShaderProgramBufferPath(outputPath, stage, useStageSuffix);
                    string csoPath = GetShaderCSOPath(outputPath, stage, useStageSuffix);

                    DXCShaderCompiler.CompileToCSO(shader, stage, csoPath);
                    ShaderProgramBufferBase buffer = bufferOperation.ExecuteFromFile(csoPath, stage.ResolveStage(), platform);
                    bufferOperation.WriteToFile(buffer, shaderProgramBufferPath, platform);
                    WritePaddedCSOFile(csoPath, GetSecondaryResourcePath(shaderProgramBufferPath));
                }
            }
            else
            {
                DXCShaderCompiler.CompileStagesToCSO(shader, stages, stage =>
                    GetShaderProgramBufferPath(outputPath, stage, useStageSuffix));
            }
        }

        if (resource is ShaderProgramBufferBPR shaderProgramBuffer && platform == Platform.BPR)
        {
            if (shaderProgramBuffer.CompiledShaderBytecode.Length > 0)
            {
                string secondaryCSOPath = GetSecondaryResourcePath(outputPath);
                WritePaddedCSOBytes(shaderProgramBuffer.CompiledShaderBytecode, secondaryCSOPath);
            }
        }

        return Task.CompletedTask;
    }

    private static Unpacker ResolveExternalImportsUnpackerFormat(
        Resource resource,
        Unpacker? importUnpackerOverride)
    {
        return importUnpackerOverride ?? resource.Unpacker;
    }

    private static void WriteExternalImports(
        Resource resource,
        string outputPath,
        ResourceBinaryWriter writer,
        Endian endian,
        Unpacker importUnpacker,
        bool forceExternalImportsFile)
    {
        List<KeyValuePair<long, ResourceImport>> imports = resource.GetExternalImports().ToList();

        string yamlImportsPath = ResourceImport.GetImportsPath(outputPath, Unpacker.YAP);
        string datImportsPath = ResourceImport.GetImportsPath(outputPath, Unpacker.Raw);

        if (imports.Count == 0)
        {
            ResourceImport.DeleteImportsSidecarFiles(outputPath);
            return;
        }

        if (importUnpacker == Unpacker.YAP)
        {
            ResourceImport.DeleteImportsSidecarFiles(outputPath);
            WriteExternalImportsYaml(imports, yamlImportsPath);
            return;
        }

        if (forceExternalImportsFile)
        {
            ResourceImport.DeleteImportsSidecarFiles(outputPath);
            WriteExternalImportsDat(datImportsPath, endian, imports);
            return;
        }

        writer.BaseStream.Seek(0, SeekOrigin.End);
        WriteBinaryImports(writer, imports);
        ResourceImport.DeleteImportsSidecarFiles(outputPath);
    }

    private static void WriteExternalImportsYaml(
        List<KeyValuePair<long, ResourceImport>> imports,
        string importsPath)
    {
        List<string> lines = new(imports.Count);
        foreach (KeyValuePair<long, ResourceImport> entry in imports)
        {
            ulong resourceId = ResourceUtilities.ResolveResourceID(entry.Value);
            lines.Add($"- \"0x{entry.Key:x8}\": \"{resourceId:X8}\"");
        }

        File.WriteAllLines(importsPath, lines);
    }

    private static void WriteExternalImportsDat(
        string importsPath,
        Endian endianness,
        List<KeyValuePair<long, ResourceImport>> imports)
    {
        using FileStream fs = new(importsPath, FileMode.Create, FileAccess.Write);
        using EndianAwareBinaryWriter writer = new(fs, endianness);
        WriteBinaryImports(writer, imports);
    }

    private static void WriteBinaryImports(
        EndianAwareBinaryWriter writer,
        List<KeyValuePair<long, ResourceImport>> imports)
    {
        foreach (KeyValuePair<long, ResourceImport> entry in imports)
        {
            if (entry.Key < 0 || entry.Key > uint.MaxValue)
            {
                throw new InvalidDataException(
                    $"Import offset 0x{entry.Key:X} cannot be stored in a binary imports block.");
            }

            // Probably overkill but I just want to make sure we always use the correct writer overloads
            writer.Write((ulong)ResourceUtilities.ResolveResourceID(entry.Value));
            writer.Write((uint)entry.Key);
            writer.Write(0x00000000);
        }
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
