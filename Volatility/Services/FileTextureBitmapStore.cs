using Volatility.Abstractions.Messaging;
using Volatility.Abstractions.Services;
using Volatility.Resources;
using Volatility.Utilities;

namespace Volatility.Services;

public sealed class FileTextureBitmapStore(
    IPathProvider pathProvider,
    IProcessRunner processRunner,
    IMessageSink messageSink)
    : ITextureBitmapStore
{
    public string GetResourceBaseName(string headerPath, Unpacker unpacker)
    {
        return GetHeaderBaseName(Path.GetFileName(headerPath), unpacker);
    }

    public string GetSecondaryBitmapPath(string headerPath, Unpacker unpacker)
    {
        string? directory = Path.GetDirectoryName(headerPath);
        string baseName = GetResourceBaseName(headerPath, unpacker);
        string secondarySuffix = GetSecondaryResourceSuffix(unpacker);

        return string.IsNullOrEmpty(directory)
            ? baseName + secondarySuffix
            : Path.Combine(directory, baseName + secondarySuffix);
    }

    public byte[] ReadNormalizedBitmapData(TextureBase texture, string bitmapPath)
    {
        return NormalizeBitmapData(texture, File.ReadAllBytes(bitmapPath));
    }

    public void WriteNormalizedBitmapFile(TextureBase texture, string sourceBitmapPath, string outputPath, bool overwrite = true)
    {
        if (!overwrite && File.Exists(outputPath))
        {
            throw new IOException($"The file '{outputPath}' already exists.");
        }

        File.WriteAllBytes(outputPath, ReadNormalizedBitmapData(texture, sourceBitmapPath));
    }

    public void ConvertPS3GTFToDDS(TexturePS3 texture, string sourceBitmapPath, string destinationBitmapPath, bool verbose = false)
    {
        byte[] header = new byte[0xE];
        using MemoryStream ps3Stream = new(header);
        using ResourceBinaryWriter writer = new(ps3Stream, texture.ResourceEndian);

        texture.WriteToStream(writer);
        ps3Stream.ReadExactly(header, 0, 0xE);

        byte[] fileBytes = File.ReadAllBytes(sourceBitmapPath);
        byte[] size = BitConverter.GetBytes(fileBytes.Length);
        Array.Reverse(size);

        byte[] gtfBytes = new byte[]
        {
            0x02, 0x02, 0x00, 0xFF,
        }
        .Concat(size)
        .Concat(new byte[]
        {
            0x00, 0x00, 0x00, 0x01,
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x80,
        })
        .Concat(size)
        .Concat(header)
        .Concat(new byte[0x5A])
        .Concat(fileBytes)
        .ToArray();

        File.WriteAllBytes($"{destinationBitmapPath}.gtf", gtfBytes);

        string gtf2ddsExecutablePath = Path.Combine(pathProvider.GetDirectory(VolatilityPathLocation.Tools), "gtf2dds.exe");
        if (!File.Exists(gtf2ddsExecutablePath))
        {
            throw new FileNotFoundException("Unable to find external tool gtf2dds.exe!");
        }

        if (verbose)
        {
            messageSink.Verbose(
                $"Running: {gtf2ddsExecutablePath} -o \"{destinationBitmapPath}.dds\" \"{destinationBitmapPath}.gtf\"",
                MessageCategory.Texture,
                nameof(FileTextureBitmapStore));
            messageSink.Verbose(
                "Converting PS3 GTF texture to DDS...",
                MessageCategory.Texture,
                nameof(FileTextureBitmapStore));
        }

        processRunner.RunAndRelayOutput(
            gtf2ddsExecutablePath,
            $"-o \"{destinationBitmapPath}.dds\" \"{destinationBitmapPath}.gtf\"");

        fileBytes = File.ReadAllBytes($"{destinationBitmapPath}.dds");
        if (fileBytes.Length <= 0x80)
        {
            throw new InvalidDataException($"Texture file '{destinationBitmapPath}.dds' is too short to contain a DDS header.");
        }

        byte[] trimmedBytes = new byte[fileBytes.Length - 0x80];
        Array.Copy(fileBytes, 0x80, trimmedBytes, 0, trimmedBytes.Length);
        File.WriteAllBytes(destinationBitmapPath, trimmedBytes);

        if (verbose)
        {
            messageSink.Verbose(
                "Trimmed converted DDS header.",
                MessageCategory.Texture,
                nameof(FileTextureBitmapStore));
        }
    }

    private static byte[] NormalizeBitmapData(TextureBase texture, byte[] bitmapData)
    {
        return texture switch
        {
            TextureX360 x360 when x360.Format.Tiled => X360TextureUtilities.GetUntiled360TextureData(x360, bitmapData),
            TexturePS3 ps3 when ps3.Format == CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_A8R8G8B8
                => PS3TextureUtilities.DecodePS3A8R8G8B8(bitmapData, ps3.Width, ps3.Height, ps3.MipmapLevels),
            _ => bitmapData,
        };
    }

    private static string GetHeaderBaseName(string headerFileName, Unpacker unpacker)
    {
        string primarySuffix = GetPrimaryResourceSuffix(unpacker);
        return headerFileName.EndsWith(primarySuffix, StringComparison.OrdinalIgnoreCase)
            ? headerFileName[..^primarySuffix.Length]
            : Path.GetFileNameWithoutExtension(headerFileName);
    }

    private static string GetPrimaryResourceSuffix(Unpacker unpacker)
    {
        return unpacker switch
        {
            Unpacker.Bnd2Manager => "_1.bin",
            Unpacker.DGI => ".dat",
            Unpacker.YAP => "_primary.dat",
            Unpacker.Raw => ".dat",
            Unpacker.Volatility => throw new NotImplementedException(),
            _ => throw new NotImplementedException(),
        };
    }

    private static string GetSecondaryResourceSuffix(Unpacker unpacker)
    {
        return unpacker switch
        {
            Unpacker.Bnd2Manager => "_2.bin",
            Unpacker.DGI => "_texture.dat",
            Unpacker.YAP => "_secondary.dat",
            Unpacker.Raw => "_texture.dat",
            Unpacker.Volatility => throw new NotImplementedException(),
            _ => throw new NotImplementedException(),
        };
    }
}
