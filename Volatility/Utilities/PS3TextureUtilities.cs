using System.Diagnostics;

using Volatility.Resources;

using static Volatility.Utilities.EnvironmentUtilities;

namespace Volatility.Utilities;

public static class PS3TextureUtilities
{
    public static uint CalculatePitchPS3(int width, int blockSize)
    {
        return (uint)(((width + 3) / 4) * blockSize);
    }

    public static byte[] DecodePS3A8R8G8B8(byte[] sourceData, int width, int height, int mipmapCount)
    {
        int levelCount = Math.Max(1, mipmapCount);
        int totalSize = 0;

        for (int mip = 0; mip < levelCount; mip++)
        {
            int mipWidth = Math.Max(1, width >> mip);
            int mipHeight = Math.Max(1, height >> mip);
            totalSize += checked(mipWidth * mipHeight * 4);
        }

        if (sourceData.Length < totalSize)
        {
            throw new InvalidDataException($"PS3 A8R8G8B8 bitmap is too short. Expected at least {totalSize} bytes, got {sourceData.Length}.");
        }

        byte[] linearData = new byte[totalSize];
        int sourceOffset = 0;
        int destinationOffset = 0;

        for (int mip = 0; mip < levelCount; mip++)
        {
            int mipWidth = Math.Max(1, width >> mip);
            int mipHeight = Math.Max(1, height >> mip);
            int mipSize = checked(mipWidth * mipHeight * 4);

            DecodeMortonMipLevel
            (
                sourceData.AsSpan(sourceOffset, mipSize),
                linearData.AsSpan(destinationOffset, mipSize),
                mipWidth,
                mipHeight,
                4
            );

            sourceOffset += mipSize;
            destinationOffset += mipSize;
        }

        return linearData;
    }

    public static byte[] EncodePS3A8R8G8B8(byte[] sourceData, int width, int height, int mipmapCount)
    {
        int levelCount = Math.Max(1, mipmapCount);
        int totalSize = 0;

        for (int mip = 0; mip < levelCount; mip++)
        {
            int mipWidth = Math.Max(1, width >> mip);
            int mipHeight = Math.Max(1, height >> mip);
            totalSize += checked(mipWidth * mipHeight * 4);
        }

        if (sourceData.Length < totalSize)
        {
            throw new InvalidDataException($"Linear A8R8G8B8 bitmap is too short. Expected at least {totalSize} bytes, got {sourceData.Length}.");
        }

        byte[] mortonData = new byte[totalSize];
        int sourceOffset = 0;
        int destinationOffset = 0;

        for (int mip = 0; mip < levelCount; mip++)
        {
            int mipWidth = Math.Max(1, width >> mip);
            int mipHeight = Math.Max(1, height >> mip);
            int mipSize = checked(mipWidth * mipHeight * 4);

            EncodeMortonMipLevel
            (
                sourceData.AsSpan(sourceOffset, mipSize),
                mortonData.AsSpan(destinationOffset, mipSize),
                mipWidth,
                mipHeight,
                4
            );

            sourceOffset += mipSize;
            destinationOffset += mipSize;
        }

        return mortonData;
    }

    public static void PS3GTFToDDS(TexturePS3 ps3Header, string sourceBitmapPath, string destinationBitmapPath, bool verbose = false)
    {
        byte[] header = new byte[0xE];
        using MemoryStream ps3Stream = new(header);
        using ResourceBinaryWriter writer = new(ps3Stream, ps3Header.ResourceEndian);
                
        ps3Header.WriteToStream(writer);
        ps3Stream.ReadExactly(header, 0, 0xE);

        writer.Close();
        ps3Stream.Close();

        PS3GTFToDDS(header, sourceBitmapPath, destinationBitmapPath, verbose); 
    }

    public static void PS3GTFToDDS(string ps3HeaderPath, string sourceBitmapPath, string destinationBitmapPath, bool verbose = false)
    {
        using FileStream ps3Stream = new(ps3HeaderPath, FileMode.Open, FileAccess.Read);
        using BinaryReader reader = new(ps3Stream);

        byte[] ps3Header = reader.ReadBytes(0xE);

        reader.Close();
        ps3Stream.Close();

        PS3GTFToDDS(ps3Header, sourceBitmapPath, destinationBitmapPath, verbose);
    }

    public static void PS3GTFToDDS(byte[] ps3Header, string sourceBitmapPath, string destinationBitmapPath, bool verbose = false)
    {
        Array.ConstrainedCopy(ps3Header, 0, ps3Header, 0, 0xE);

        byte[] fileBytes = File.ReadAllBytes(sourceBitmapPath);
        byte[] size = BitConverter.GetBytes(fileBytes.Length);
        Array.Reverse(size);

        byte[] gtf = new byte[]
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
        .Concat(ps3Header)
        .Concat(new byte[0x5A])
        .Concat(fileBytes)
        .ToArray();

        File.WriteAllBytes($"{destinationBitmapPath}.gtf", gtf);

        string gtf2ddsPath = Path.Combine
        (
            GetEnvironmentDirectory(EnvironmentDirectory.Tools),
            "gtf2dds.exe"
        );

        if (!File.Exists(gtf2ddsPath))
        {
            throw new FileNotFoundException("Unable to find external tool gtf2dds.exe!");
        }

        if (verbose) Console.WriteLine($"Running: {gtf2ddsPath} -o \"{destinationBitmapPath}.dds\" \"{destinationBitmapPath}.gtf\"");
        if (verbose) Console.WriteLine("Converting PS3 GTF texture to DDS...");

        ProcessUtilities.RunAndRelayOutput(
            gtf2ddsPath,
            $"-o \"{destinationBitmapPath}.dds\" \"{destinationBitmapPath}.gtf\"");

        fileBytes = File.ReadAllBytes($"{destinationBitmapPath}.dds");

        if (fileBytes.Length > 0x80)
        {
            byte[] newBytes = new byte[fileBytes.Length - 0x80];
            Array.Copy(fileBytes, 0x80, newBytes, 0, newBytes.Length);

            try
            {
                File.WriteAllBytes(destinationBitmapPath, newBytes);
            }
            catch (IOException e)
            {
                Console.WriteLine($"Error trying to write trimmed DDS data for {Path.GetFileNameWithoutExtension(sourceBitmapPath)}: {e.Message}");
            }

            if (verbose) Console.WriteLine("Trimmed converted DDS header.");
        }
        else
        {
            Console.WriteLine($"Error trying to write trimmed DDS data for {Path.GetFileNameWithoutExtension(sourceBitmapPath)}: Texture file is too short! Not a DDS file.");
        }
    }

    private static void DecodeMortonMipLevel(ReadOnlySpan<byte> sourceData, Span<byte> destinationData, int width, int height, int bytesPerPixel)
    {
        (uint MortonIndex, int LinearOffset)[] pixelOrder = new (uint MortonIndex, int LinearOffset)[width * height];
        int orderIndex = 0;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                pixelOrder[orderIndex++] =
                (
                    EncodeMorton2D((uint)x, (uint)y),
                    ((y * width) + x) * bytesPerPixel
                );
            }
        }

        Array.Sort(pixelOrder, static (left, right) => left.MortonIndex.CompareTo(right.MortonIndex));

        for (int i = 0; i < pixelOrder.Length; i++)
        {
            int sourceOffset = i * bytesPerPixel;
            sourceData.Slice(sourceOffset, bytesPerPixel).CopyTo(destinationData.Slice(pixelOrder[i].LinearOffset, bytesPerPixel));
        }
    }

    private static void EncodeMortonMipLevel(ReadOnlySpan<byte> sourceData, Span<byte> destinationData, int width, int height, int bytesPerPixel)
    {
        (uint MortonIndex, int LinearOffset)[] pixelOrder = new (uint MortonIndex, int LinearOffset)[width * height];
        int orderIndex = 0;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                pixelOrder[orderIndex++] =
                (
                    EncodeMorton2D((uint)x, (uint)y),
                    ((y * width) + x) * bytesPerPixel
                );
            }
        }

        Array.Sort(pixelOrder, static (left, right) => left.MortonIndex.CompareTo(right.MortonIndex));

        for (int i = 0; i < pixelOrder.Length; i++)
        {
            int destinationOffset = i * bytesPerPixel;
            sourceData.Slice(pixelOrder[i].LinearOffset, bytesPerPixel).CopyTo(destinationData.Slice(destinationOffset, bytesPerPixel));
        }
    }

    private static uint EncodeMorton2D(uint x, uint y)
    {
        return Part1By1(x) | (Part1By1(y) << 1);
    }

    private static uint Part1By1(uint value)
    {
        value &= 0x0000FFFF;
        value = (value | (value << 8)) & 0x00FF00FF;
        value = (value | (value << 4)) & 0x0F0F0F0F;
        value = (value | (value << 2)) & 0x33333333;
        value = (value | (value << 1)) & 0x55555555;
        return value;
    }
}
