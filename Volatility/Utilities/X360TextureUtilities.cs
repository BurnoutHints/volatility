using Volatility.Resource.TextureHeader;

namespace Volatility.Utilities;

internal class X360TextureUtilities
{
    public static void ConvertMipmapsToX360(TextureHeaderBase header, GPUTEXTUREFORMAT format, string inputPath, string outputPath)
    {
        using var stream = new FileStream(inputPath, FileMode.Open, FileAccess.Read);
        using var reader = new BinaryReader(stream);

        int width = header.Width;
        int height = header.Height;
        int mipMapCount = header.MipmapLevels;
        var mipmaps = new byte[mipMapCount][];

        for (int i = 0; i < mipMapCount; i++)
        {
            int mipSize = CalculateMipSize(width, height, DataUtilities.CalculatePitchX360((ushort)width, (ushort)height), format);
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

    private static byte[][] AlignAndPackMipmaps(byte[][] mipmaps, TextureHeaderBase textureInfo, GPUTEXTUREFORMAT format)
    {
        const int alignment = 4096;
        var alignedMipmaps = new byte[mipmaps.Length][];
        int totalSize = 0;

        for (int i = 0; i < mipmaps.Length; i++)
        {
            int mipSize = mipmaps[i].Length;
            int paddedSize = ((mipSize + alignment - 1) / alignment) * alignment;

            alignedMipmaps[i] = new byte[paddedSize];
            Array.Copy(mipmaps[i], alignedMipmaps[i], mipSize);

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

    public static void WriteUntiled360TextureFile(TextureHeaderX360 xboxHeader, string textureBitmapPath, string outPath = "")
    {
        if (string.IsNullOrEmpty(outPath))
        {
            outPath = textureBitmapPath;
        }
        if (xboxHeader.Format.Tiled)
        {
            FileStream stream = new FileStream(textureBitmapPath, FileMode.Open, FileAccess.Read);
            byte[] bitmapData = new byte[stream.Length];
            
            stream.Read(bitmapData);
            bitmapData = ConvertToLinearTexture(bitmapData, (int)xboxHeader.Format.Size.Width, (int)xboxHeader.Format.Size.Height, xboxHeader.Format.DataFormat);
            stream.Close();
            
            stream = new FileStream(outPath, FileMode.OpenOrCreate, FileAccess.Write);
            stream.Write(bitmapData);
            stream.Close();
        }
    }

    // THE BELOW CODE IS CREDITED TO NCDyson for RareView
    // AND "Pimpin Tyler and Anthony" for GTA IV Xbox 360 Texture Editor

    // It originated from the GTA IV Xbox 360 Texture Editor,
    // in which its source code was released publicly.

    // I borrowed it from RareView as links to the GTA version seem to be dead.

    // THERE WAS NO PROPER LICENSE FOR IT THOUGH, I DID NOT WRITE IT!!

    // Its calculations are accurate to what the Xbox 360 does officially,
    // so the output would be identical whether I spent the insane amount
    // of hours figuring it out and writing it opposed to not reinventing the wheel.

    private static byte[] ConvertToLinearTexture(byte[] data, int _width, int _height, GPUTEXTUREFORMAT _textureFormat)
    {
        byte[] destData = new byte[data.Length];

        int blockSize;
        int texelPitch;

        switch (_textureFormat)
        {
            case GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_8_8:
                blockSize = 1;
                texelPitch = 2;
                break;
            case GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_8:
                blockSize = 1;
                texelPitch = 1;
                break;
            case GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT1:
                blockSize = 4;
                texelPitch = 8;
                break;
            case GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT2_3:
            case GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT4_5:
            case GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXN:
                blockSize = 4;
                texelPitch = 16;
                break;
            case GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_8_8_8_8:
                blockSize = 1;
                texelPitch = 4;
                break;
            case GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_4_4_4_4:
                blockSize = 1;
                texelPitch = 2;
                break;
            case GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_5_6_5:
                blockSize = 1;
                texelPitch = 2;
                break;
            default:
                throw new ArgumentOutOfRangeException("Bad texture type!");
        }

        int blockWidth = _width / blockSize;
        int blockHeight = _height / blockSize;

        for (int j = 0; j < blockHeight; j++)
        {
            for (int i = 0; i < blockWidth; i++)
            {
                int blockOffset = j * blockWidth + i;

                int x = XGAddress2DTiledX(blockOffset, blockWidth, texelPitch);
                int y = XGAddress2DTiledY(blockOffset, blockWidth, texelPitch);

                int srcOffset = j * blockWidth * texelPitch + i * texelPitch;
                int destOffset = y * blockWidth * texelPitch + x * texelPitch;
                //TODO: ConvertToLinearTexture apparently breaks on on textures with a height of 64...
                if (destOffset >= destData.Length) continue;
                Array.Copy(data, srcOffset, destData, destOffset, texelPitch);
            }
        }

        return destData;
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
