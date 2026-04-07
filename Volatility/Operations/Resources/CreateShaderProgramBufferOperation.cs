using Volatility.Resources;
using Volatility.Utilities;

namespace Volatility.Operations.Resources;

internal sealed class CreateShaderProgramBufferOperation
{
    public ShaderProgramBufferBase ExecuteFromFile(string csoPath, ShaderStageType stage, Platform platform)
    {
        if (string.IsNullOrWhiteSpace(csoPath))
            throw new ArgumentNullException(nameof(csoPath));
        if (!File.Exists(csoPath))
            throw new FileNotFoundException($"CSO file not found: {csoPath}");

        byte[] csoBytes = File.ReadAllBytes(csoPath);
        return Execute(csoBytes, stage, platform);
    }

    public ShaderProgramBufferBase Execute(byte[] csoBytes, ShaderStageType stage, Platform platform)
    {
        return platform switch
        {
            Platform.BPR => ShaderProgramBufferBPR.FromCSO(csoBytes, stage),
            _ => throw new NotSupportedException($"ShaderProgramBuffer is not implemented for platform {platform}.")
        };
    }

    public ShaderProgramBufferBPR ExecuteFromFile(string csoPath, ShaderStageType stage)
    {
        return (ShaderProgramBufferBPR)ExecuteFromFile(csoPath, stage, Platform.BPR);
    }

    public ShaderProgramBufferBPR Execute(byte[] csoBytes, ShaderStageType stage)
    {
        return (ShaderProgramBufferBPR)Execute(csoBytes, stage, Platform.BPR);
    }

    public void WriteToFile(ShaderProgramBufferBase buffer, string outputPath, Platform platform)
    {
        if (buffer == null)
            throw new ArgumentNullException(nameof(buffer));

        ValidatePlatform(platform);

        Platform bufferPlatform = buffer.GetResourcePlatform();
        if (bufferPlatform != Platform.Agnostic && bufferPlatform != platform)
            throw new InvalidOperationException($"ShaderProgramBuffer platform mismatch: buffer={bufferPlatform}, requested={platform}.");

        string? directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using FileStream fs = new(outputPath, FileMode.Create);
        Endian endian = buffer.GetResourceEndian();
        if (endian == Endian.Agnostic)
            endian = EndianMapping.GetDefaultEndian(platform);

        using EndianAwareBinaryWriter writer = new(fs, endian);
        buffer.WriteToStream(writer, endian);
    }

    public void WriteToFile(ShaderProgramBufferBase buffer, string outputPath)
    {
        WriteToFile(buffer, outputPath, Platform.BPR);
    }

    private static void ValidatePlatform(Platform platform)
    {
        if (platform == Platform.BPR)
            return;

        throw new NotSupportedException($"ShaderProgramBuffer is not implemented for platform {platform}.");
    }
}
