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

            DecodeMortonMipLevel(
                sourceData.AsSpan(sourceOffset, mipSize),
                linearData.AsSpan(destinationOffset, mipSize),
                mipWidth,
                mipHeight,
                4);

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

            EncodeMortonMipLevel(
                sourceData.AsSpan(sourceOffset, mipSize),
                mortonData.AsSpan(destinationOffset, mipSize),
                mipWidth,
                mipHeight,
                4);

            sourceOffset += mipSize;
            destinationOffset += mipSize;
        }

        return mortonData;
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
