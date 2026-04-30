using System.Text;
using Volatility.Abstractions.Services;
using Volatility.Resources;

namespace Volatility.Services;

public sealed class FileShaderSourceStore : IShaderSourceStore
{
    public void MaterializeImportedSource(ShaderBase shader, string resourcesDirectory)
    {
        if (string.IsNullOrWhiteSpace(shader.ImportedShaderSourceText))
        {
            return;
        }

        string outputPath = ResolveOutputPath(shader, resourcesDirectory);
        if (File.Exists(outputPath))
        {
            shader.ImportedShaderSourceText = null;
            return;
        }

        string? directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(outputPath, shader.ImportedShaderSourceText, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        shader.ImportedShaderSourceText = null;
    }

    private static string ResolveOutputPath(ShaderBase shader, string resourcesDirectory)
    {
        if (string.IsNullOrWhiteSpace(shader.ShaderSourcePath))
        {
            string baseName = !string.IsNullOrWhiteSpace(shader.AssetName)
                ? shader.AssetName
                : !string.IsNullOrWhiteSpace(shader.ImportedFileName)
                    ? Path.GetFileNameWithoutExtension(shader.ImportedFileName)
                    : "shader";

            string sanitizedName = ShaderBase.ShaderPathSanitizer().Replace(baseName, string.Empty);
            if (string.IsNullOrWhiteSpace(sanitizedName))
            {
                sanitizedName = "shader";
            }

            shader.ShaderSourcePath = $"{sanitizedName}.{ResourceType.Shader}.hlsl";
        }

        return Path.IsPathRooted(shader.ShaderSourcePath)
            ? shader.ShaderSourcePath
            : Path.Combine(resourcesDirectory, shader.ShaderSourcePath);
    }
}
