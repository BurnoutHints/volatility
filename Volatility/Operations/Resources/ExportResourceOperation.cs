using Volatility.Resources;

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

        return Task.CompletedTask;
    }
}
