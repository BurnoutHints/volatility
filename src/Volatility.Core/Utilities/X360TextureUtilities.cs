using Volatility.Resources;

namespace Volatility.Utilities;

internal class X360TextureUtilities
{
    public static ushort CalculatePitchX360(ushort width, ushort height)
    {
        return (ushort)(DataUtilities.Clamp(width, 128, width) / 32);
    }

    public static uint CalculateMipAddressX360(uint width, uint height)
    {
        return (width * height) / 4096;
    }

    public static void ConvertMipmapsToX360(TextureBase header, GPUTEXTUREFORMAT format, string inputPath, string outputPath)
    {
        using var stream = new FileStream(inputPath, FileMode.Open, FileAccess.Read);
        using var reader = new BinaryReader(stream);

        int width = header.Width;
        int height = header.Height;
        int mipMapCount = header.MipmapLevels;
        var mipmaps = new byte[mipMapCount][];

        for (int i = 0; i < mipMapCount; i++)
        {
            int mipSize = CalculateMipSize(width, height, CalculatePitchX360((ushort)width, (ushort)height), format);
            mipmaps[i] = reader.ReadBytes(mipSize);

            width = Math.Max(1, width / 2);
            height = Math.Max(1, height / 2);
        }

        var alignedMipmaps = AlignAndPackMipmaps(mipmaps, header, format);

        using var outputStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
        using var writer = new BinaryWriter(outputStream);

        foreach (var mip in alignedMipmaps)
        {
            if (mip != null)
            {
                writer.Write(mip);
            }
        }
    }

    private static byte[]?[] AlignAndPackMipmaps(byte[][] mipmaps, TextureBase textureInfo, GPUTEXTUREFORMAT format)
    {
        const int alignment = 4096;
        byte[]?[] alignedMipmaps = new byte[mipmaps.Length][];
        int totalSize = 0;

        for (int i = 0; i < mipmaps.Length; i++)
        {
            int mipSize = mipmaps[i].Length;
            int paddedSize = ((mipSize + alignment - 1) / alignment) * alignment;

            alignedMipmaps[i] = new byte[paddedSize];
            Array.Copy(mipmaps[i], alignedMipmaps[i]!, mipSize);

            totalSize += paddedSize;

            if (ShouldPackMipLevel(mipmaps[i], format))
            {
                alignedMipmaps[i] = PackMipmaps(mipmaps, i, format, out totalSize);
                for (int j = i + 1; j < mipmaps.Length; j++)
                {
                    alignedMipmaps[j] = null;
                }
                break;
            }
        }

        totalSize = ((totalSize + alignment - 1) / alignment) * alignment;

        return alignedMipmaps;
    }

    private static bool ShouldPackMipLevel(byte[] mipLevel, GPUTEXTUREFORMAT format)
    {
        int tileSize = format switch
        {
            GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT1
            or GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT1_AS_16_16_16_16
            or GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT2_3
            or GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT2_3_AS_16_16_16_16
            or GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT4_5
            or GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT4_5_AS_16_16_16_16
            or GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT5A => 128, // DXT tile size is 128
            _ => 32, // Uncompressed tile size is 32
        };

        return mipLevel.Length <= tileSize * tileSize;
    }

    private static byte[] PackMipmaps(byte[][] mipmaps, int startLevel, GPUTEXTUREFORMAT format, out int totalSize)
    {
        const int tileSize = 4096;
        totalSize = 0;

        for (int i = startLevel; i < mipmaps.Length; i++)
        {
            totalSize += mipmaps[i].Length;
        }

        totalSize = ((totalSize + tileSize - 1) / tileSize) * tileSize;
        byte[] packedMipmaps = new byte[totalSize];

        int offset = 0;
        for (int i = startLevel; i < mipmaps.Length; i++)
        {
            Array.Copy(mipmaps[i], 0, packedMipmaps, offset, mipmaps[i].Length);
            offset += mipmaps[i].Length;
        }

        return packedMipmaps;
    }

    private static int CalculateMipSize(int width, int height, int pitch, GPUTEXTUREFORMAT format)
    {
        return format switch
        {
            GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT1
            or GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT1_AS_16_16_16_16 => ((width + 3) / 4) * ((height + 3) / 4) * 8, // DXT1: 8 bytes per 4x4 block
            GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT2_3
            or GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT2_3_AS_16_16_16_16
            or GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT4_5
            or GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT4_5_AS_16_16_16_16
            or GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT5A => ((width + 3) / 4) * ((height + 3) / 4) * 16, // DXT2/3/4/5: 16 bytes per 4x4 block
            _ => width * height * pitch,
        };
    }

    public static void WriteUntiled360TextureFile(TextureX360 xboxHeader, string textureBitmapPath, string outPath = "")
    {
        if (string.IsNullOrEmpty(outPath))
        {
            outPath = textureBitmapPath;
        }
        if (!xboxHeader.Format.Tiled)
        {
            return;
        }

        byte[] bitmapData = File.ReadAllBytes(textureBitmapPath);
        bitmapData = GetUntiled360TextureData(xboxHeader, bitmapData);
        File.WriteAllBytes(outPath, bitmapData);
    }

    public static byte[] GetUntiled360TextureData(TextureX360 xboxHeader, byte[] bitmapData)
    {
        return xboxHeader.Format.Tiled
            ? ConvertToLinearTexture(bitmapData, xboxHeader.Width, xboxHeader.Height, xboxHeader.MipmapLevels, xboxHeader.Format.DataFormat)
            : bitmapData;
    }

    public static byte[] GetTiled360TextureData(TextureX360 xboxHeader, byte[] bitmapData)
    {
        return xboxHeader.Format.Tiled
            ? ConvertToTiledTexture(bitmapData, xboxHeader.Width, xboxHeader.Height, xboxHeader.MipmapLevels, xboxHeader.Format.DataFormat)
            : bitmapData;
    }

    // X360 GPU surfaces are stored as 16 bit big endian words. 
    public static void SwapEndian8in16(byte[] data)
    {
        int count = data.Length & ~1;
        for (int i = 0; i < count; i += 2)
        {
            (data[i], data[i + 1]) = (data[i + 1], data[i]);
        }
    }

    private static (int BlockSize, int TexelPitch) GetX360BlockInfo(GPUTEXTUREFORMAT format)
    {
        return format switch
        {
            GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_8
            or GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_8_A
            or GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_8_B
            or GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_1 => (1, 1),

            GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_8_8
            or GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_4_4_4_4
            or GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_5_6_5
            or GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_1_5_5_5
            or GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_6_5_5
            or GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_16
            or GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_16_FLOAT => (1, 2),

            GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_8_8_8_8
            or GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_2_10_10_10
            or GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_16_16
            or GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_16_16_FLOAT
            or GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_32
            or GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_32_FLOAT => (1, 4),

            GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_16_16_16_16
            or GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_16_16_16_16_FLOAT
            or GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_32_32 => (1, 8),

            GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT1
            or GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT1_AS_16_16_16_16
            or GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT3A
            or GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT5A
            or GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_CTX1 => (4, 8),

            GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT2_3
            or GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT2_3_AS_16_16_16_16
            or GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT4_5
            or GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT4_5_AS_16_16_16_16
            or GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXN => (4, 16),

            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported X360 texture format for detiling."),
        };
    }

    // The 360 stores a tiled surface padded to 32-block tiles in BOTH dimensions, so the source
    // mip's storage is alignedBlockWidth x alignedBlockHeight blocks. We walk every block of that
    // padded grid, map its tiled-storage offset back to its linear (x, y) with the inverse address
    // math below, drop the padding blocks that fall outside the real WxH, and write the rest into a
    // tightly-packed linear mip. Each subsequent mip is read from the next aligned-tiled region
    // (BaseAddress/MipAddress packing aside; this assumes consecutive per-mip tiled storage, which
    // is the common case -- mip 0, the surface that matters most, is always correct). Bounds are
    // clamped so a short/odd buffer can never throw or scatter out of range.
    private static byte[] ConvertToLinearTexture(byte[] data, int width, int height, int mipCount, GPUTEXTUREFORMAT format)
    {
        (int blockSize, int texelPitch) = GetX360BlockInfo(format);

        using MemoryStream linearStream = new();
        int srcMipOffset = 0;
        int mipWidth = width;
        int mipHeight = height;

        for (int mip = 0; mip < Math.Max(1, mipCount); mip++)
        {
            if (srcMipOffset >= data.Length)
            {
                break;
            }

            int blockWidth = Math.Max(1, mipWidth / blockSize);
            int blockHeight = Math.Max(1, mipHeight / blockSize);
            int alignedBlockWidth = (blockWidth + 31) & ~31;
            int alignedBlockHeight = (blockHeight + 31) & ~31;

            int linearMipBytes = blockWidth * blockHeight * texelPitch;
            byte[] linearMip = new byte[linearMipBytes];

            int tiledBlockCount = alignedBlockWidth * alignedBlockHeight;
            for (int tiledOffset = 0; tiledOffset < tiledBlockCount; tiledOffset++)
            {
                int x = XGAddress2DTiledX(tiledOffset, blockWidth, texelPitch);
                int y = XGAddress2DTiledY(tiledOffset, blockWidth, texelPitch);

                // Skip the tile padding that falls outside the real surface.
                if (x >= blockWidth || y >= blockHeight)
                {
                    continue;
                }

                int srcOffset = srcMipOffset + tiledOffset * texelPitch;
                int destOffset = (y * blockWidth + x) * texelPitch;
                if (srcOffset + texelPitch > data.Length || destOffset + texelPitch > linearMipBytes)
                {
                    continue;
                }

                Array.Copy(data, srcOffset, linearMip, destOffset, texelPitch);
            }

            linearStream.Write(linearMip, 0, linearMipBytes);

            srcMipOffset += alignedBlockWidth * alignedBlockHeight * texelPitch;
            mipWidth = Math.Max(1, mipWidth / 2);
            mipHeight = Math.Max(1, mipHeight / 2);
        }

        return linearStream.ToArray();
    }

    private static byte[] ConvertToTiledTexture(byte[] data, int width, int height, int mipCount, GPUTEXTUREFORMAT format)
    {
        (int blockSize, int texelPitch) = GetX360BlockInfo(format);

        using MemoryStream tiledStream = new();
        int srcMipOffset = 0;
        int mipWidth = width;
        int mipHeight = height;

        for (int mip = 0; mip < Math.Max(1, mipCount); mip++)
        {
            if (srcMipOffset >= data.Length)
            {
                break;
            }

            int blockWidth = Math.Max(1, mipWidth / blockSize);
            int blockHeight = Math.Max(1, mipHeight / blockSize);
            int alignedBlockWidth = (blockWidth + 31) & ~31;
            int alignedBlockHeight = (blockHeight + 31) & ~31;

            int linearMipBytes = blockWidth * blockHeight * texelPitch;

            int tiledBlockCount = alignedBlockWidth * alignedBlockHeight;
            byte[] tiledMip = new byte[tiledBlockCount * texelPitch];

            for (int tiledOffset = 0; tiledOffset < tiledBlockCount; tiledOffset++)
            {
                int x = XGAddress2DTiledX(tiledOffset, blockWidth, texelPitch);
                int y = XGAddress2DTiledY(tiledOffset, blockWidth, texelPitch);

                // Skip the tile padding that has no corresponding linear source block.
                if (x >= blockWidth || y >= blockHeight)
                {
                    continue;
                }

                int srcOffset = srcMipOffset + (y * blockWidth + x) * texelPitch;
                int destOffset = tiledOffset * texelPitch;
                if (srcOffset + texelPitch > data.Length || destOffset + texelPitch > tiledMip.Length)
                {
                    continue;
                }

                Array.Copy(data, srcOffset, tiledMip, destOffset, texelPitch);
            }

            tiledStream.Write(tiledMip, 0, tiledMip.Length);

            srcMipOffset += linearMipBytes;
            mipWidth = Math.Max(1, mipWidth / 2);
            mipHeight = Math.Max(1, mipHeight / 2);
        }

        return tiledStream.ToArray();
    }

    private static int XGAddress2DTiledX(int Offset, int Width, int TexelPitch)
    {
        int AlignedWidth = (Width + 31) & ~31;

        int LogBpp = (TexelPitch >> 2) + ((TexelPitch >> 1) >> (TexelPitch >> 2));
        int OffsetB = Offset << LogBpp;
        int OffsetT = ((OffsetB & ~4095) >> 3) + ((OffsetB & 1792) >> 2) + (OffsetB & 63);
        int OffsetM = OffsetT >> (7 + LogBpp);

        int MacroX = ((OffsetM % (AlignedWidth >> 5)) << 2);
        int Tile = ((((OffsetT >> (5 + LogBpp)) & 2) + (OffsetB >> 6)) & 3);
        int Macro = (MacroX + Tile) << 3;
        int Micro = ((((OffsetT >> 1) & ~15) + (OffsetT & 15)) & ((TexelPitch << 3) - 1)) >> LogBpp;

        return Macro + Micro;
    }

    private static int XGAddress2DTiledY(int Offset, int Width, int TexelPitch)
    {
        int AlignedWidth = (Width + 31) & ~31;

        int LogBpp = (TexelPitch >> 2) + ((TexelPitch >> 1) >> (TexelPitch >> 2));
        int OffsetB = Offset << LogBpp;
        int OffsetT = ((OffsetB & ~4095) >> 3) + ((OffsetB & 1792) >> 2) + (OffsetB & 63);
        int OffsetM = OffsetT >> (7 + LogBpp);

        int MacroY = ((OffsetM / (AlignedWidth >> 5)) << 2);
        int Tile = ((OffsetT >> (6 + LogBpp)) & 1) + (((OffsetB & 2048) >> 10));
        int Macro = (MacroY + Tile) << 3;
        int Micro = ((((OffsetT & (((TexelPitch << 6) - 1) & ~31)) + ((OffsetT & 15) << 1)) >> (3 + LogBpp)) & ~1);

        return Macro + Micro + ((OffsetT & 16) >> 4);
    }
}
