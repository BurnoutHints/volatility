using Volatility.Abstractions.Operations;
using Volatility.Operations;
using Volatility.Resources;
using Volatility.Utilities;

namespace Volatility.Operations.Resources;

internal sealed class CreateShaderProgramBufferOperation
    : IOperation<CreateShaderProgramBufferRequest, CreateShaderProgramBufferResult>
{
    public async Task<OperationResult<CreateShaderProgramBufferResult>> ExecuteAsync(
        CreateShaderProgramBufferRequest request,
        IProgress<OperationProgress>? progress,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            byte[] csoBytes;
            if (request.CsoBytes != null)
            {
                csoBytes = request.CsoBytes;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(request.CsoPath))
                {
                    return OperationResultFactory.Failure<CreateShaderProgramBufferResult>(
                        "shader_program_buffer_missing_input",
                        "Either a CSO path or CSO bytes must be provided.",
                        nameof(CreateShaderProgramBufferOperation));
                }

                if (!File.Exists(request.CsoPath))
                {
                    return OperationResultFactory.Failure<CreateShaderProgramBufferResult>(
                        "shader_program_buffer_missing_file",
                        $"CSO file not found: {request.CsoPath}",
                        nameof(CreateShaderProgramBufferOperation));
                }

                csoBytes = await File.ReadAllBytesAsync(request.CsoPath, cancellationToken);
            }

            ShaderProgramBufferBase buffer = CreateBuffer(csoBytes, request.Stage, request.Platform);
            progress?.Report(new OperationProgress("create-shader-program-buffer", 1.0, request.CsoPath));
            return OperationResultFactory.Success(new CreateShaderProgramBufferResult(buffer));
        }
        catch (Exception ex)
        {
            return OperationResultFactory.Failure<CreateShaderProgramBufferResult>(
                "create_shader_program_buffer_failed",
                ex.Message,
                nameof(CreateShaderProgramBufferOperation));
        }
    }

    public static void WriteToFile(ShaderProgramBufferBase buffer, string outputPath, Platform platform)
    {
        ArgumentNullException.ThrowIfNull(buffer);

        ValidatePlatform(platform);

        Platform bufferPlatform = buffer.ResourcePlatform;
        if (bufferPlatform != Platform.Agnostic && bufferPlatform != platform)
        {
            throw new InvalidOperationException($"ShaderProgramBuffer platform mismatch: buffer={bufferPlatform}, requested={platform}.");
        }

        string? directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using FileStream fs = new(outputPath, FileMode.Create);
        Endian endian = buffer.ResourceEndian;
        if (endian == Endian.Agnostic)
        {
            endian = EndianMapping.GetDefaultEndian(platform);
        }

        using ResourceBinaryWriter writer = new(fs, endian);
        buffer.WriteToStream(writer, endian);
    }

    private static ShaderProgramBufferBase CreateBuffer(byte[] csoBytes, ShaderStageType stage, Platform platform)
    {
        return platform switch
        {
            Platform.BPR => ShaderProgramBufferBPR.FromCSO(csoBytes, stage),
            _ => throw new NotSupportedException($"ShaderProgramBuffer is not implemented for platform {platform}.")
        };
    }

    private static void ValidatePlatform(Platform platform)
    {
        if (platform == Platform.BPR)
        {
            return;
        }

        throw new NotSupportedException($"ShaderProgramBuffer is not implemented for platform {platform}.");
    }
}

public sealed record CreateShaderProgramBufferRequest(
    ShaderStageType Stage,
    Platform Platform,
    string? CsoPath = null,
    byte[]? CsoBytes = null) : IOperationRequest;

public sealed record CreateShaderProgramBufferResult(ShaderProgramBufferBase Buffer);
