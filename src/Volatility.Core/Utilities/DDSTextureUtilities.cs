using Volatility.Resources;

namespace Volatility.Utilities;

public static class DDSTextureUtilities
{
    public static byte[] CreateDDSFile(TextureBase texture, byte[] bitmapData)
    {
        byte[] outputData = (byte[])bitmapData.Clone();
        DDSWriteConfiguration configuration = CreateWriteConfiguration(texture, outputData);

        using MemoryStream stream = new();
        using BinaryWriter writer = new(stream);

        writer.Write(0x20534444u); // DDS
        writer.Write(124u);
        writer.Write(GetHeaderFlags(texture, configuration));
        writer.Write((uint)texture.Height);
        writer.Write((uint)texture.Width);
        writer.Write(configuration.GetPitchOrLinearSize(texture.Width));
        writer.Write(texture.Dimension == DIMENSION.DIMENSION_3D ? (uint)texture.Depth : 0u);
        writer.Write((uint)Math.Max(1, (int)texture.MipmapLevels));

        for (int i = 0; i < 11; i++)
        {
            writer.Write(0u);
        }

        WritePixelFormat(writer, configuration);
        writer.Write(GetCaps(texture));
        writer.Write(GetCaps2(texture));
        writer.Write(0u);
        writer.Write(0u);
        writer.Write(0u);

        if (configuration.UseDx10Header)
        {
            writer.Write((uint)configuration.DxgiFormat);
            writer.Write((uint)GetResourceDimension(texture.Dimension));
            writer.Write(texture.Dimension == DIMENSION.DIMENSION_CUBE ? 0x4u : 0u);
            writer.Write(GetArraySize(texture));
            writer.Write(0u);
        }

        writer.Write(outputData);
        return stream.ToArray();
    }

    public static void A8R8G8B8toB8G8R8A8(byte[] pixelData, int width, int height, int mipmapCount)
    {
        int offset = 0;
        for (int mip = 0; mip < mipmapCount; mip++)
        {
            int mipWidth = Math.Max(1, width >> mip);
            int mipHeight = Math.Max(1, height >> mip);
            int mipSize = mipWidth * mipHeight * 4;

            for (int i = offset; i < offset + mipSize; i += 4)
            {
                byte alpha = pixelData[i];
                byte red = pixelData[i + 1];
                byte green = pixelData[i + 2];
                byte blue = pixelData[i + 3];

                pixelData[i] = blue;
                pixelData[i + 1] = green;
                pixelData[i + 2] = red;
                pixelData[i + 3] = alpha;
            }

            offset += mipSize;
        }
    }

    public static void A8R8G8B8toR8G8B8A8(byte[] pixelData, int width, int height, int mipmapCount)
    {
        int offset = 0;
        for (int mip = 0; mip < mipmapCount; mip++)
        {
            int mipWidth = Math.Max(1, width >> mip);
            int mipHeight = Math.Max(1, height >> mip);
            int mipSize = mipWidth * mipHeight * 4;

            for (int i = offset; i < offset + mipSize; i += 4)
            {
                byte alpha = pixelData[i];
                byte red = pixelData[i + 1];
                byte green = pixelData[i + 2];
                byte blue = pixelData[i + 3];

                pixelData[i] = red;
                pixelData[i + 1] = green;
                pixelData[i + 2] = blue;
                pixelData[i + 3] = alpha;
            }

            offset += mipSize;
        }
    }

    public static void A8R8G8B8toA8B8G8R8(byte[] pixelData, int width, int height, int mipmapCount)
    {
        int offset = 0;
        for (int mip = 0; mip < mipmapCount; mip++)
        {
            int mipWidth = Math.Max(1, width >> mip);
            int mipHeight = Math.Max(1, height >> mip);
            int mipSize = mipWidth * mipHeight * 4;

            for (int i = offset; i < offset + mipSize; i += 4)
            {
                byte alpha = pixelData[i];
                byte red = pixelData[i + 1];
                byte green = pixelData[i + 2];
                byte blue = pixelData[i + 3];

                pixelData[i] = alpha;
                pixelData[i + 1] = blue;
                pixelData[i + 2] = green;
                pixelData[i + 3] = red;
            }

            offset += mipSize;
        }
    }

    public static void R8G8B8A8toA8R8G8B8(byte[] pixelData, int width, int height, int mipmapCount)
    {
        int offset = 0;
        for (int mip = 0; mip < mipmapCount; mip++)
        {
            int mipWidth = Math.Max(1, width >> mip);
            int mipHeight = Math.Max(1, height >> mip);
            int mipSize = mipWidth * mipHeight * 4;

            for (int i = offset; i < offset + mipSize; i += 4)
            {
                byte red = pixelData[i];
                byte green = pixelData[i + 1];
                byte blue = pixelData[i + 2];
                byte alpha = pixelData[i + 3];

                pixelData[i] = alpha;
                pixelData[i + 1] = red;
                pixelData[i + 2] = green;
                pixelData[i + 3] = blue;
            }

            offset += mipSize;
        }
    }

    public static void B8G8R8A8toA8R8G8B8(byte[] pixelData, int width, int height, int mipmapCount)
    {
        int offset = 0;
        for (int mip = 0; mip < mipmapCount; mip++)
        {
            int mipWidth = Math.Max(1, width >> mip);
            int mipHeight = Math.Max(1, height >> mip);
            int mipSize = mipWidth * mipHeight * 4;

            for (int i = offset; i < offset + mipSize; i += 4)
            {
                byte blue = pixelData[i];
                byte green = pixelData[i + 1];
                byte red = pixelData[i + 2];
                byte alpha = pixelData[i + 3];

                pixelData[i] = alpha;
                pixelData[i + 1] = red;
                pixelData[i + 2] = green;
                pixelData[i + 3] = blue;
            }

            offset += mipSize;
        }
    }

    public static void A8B8G8R8toA8R8G8B8(byte[] pixelData, int width, int height, int mipmapCount)
    {
        int offset = 0;
        for (int mip = 0; mip < mipmapCount; mip++)
        {
            int mipWidth = Math.Max(1, width >> mip);
            int mipHeight = Math.Max(1, height >> mip);
            int mipSize = mipWidth * mipHeight * 4;

            for (int i = offset; i < offset + mipSize; i += 4)
            {
                byte alpha = pixelData[i];
                byte blue = pixelData[i + 1];
                byte green = pixelData[i + 2];
                byte red = pixelData[i + 3];

                pixelData[i] = alpha;
                pixelData[i + 1] = red;
                pixelData[i + 2] = green;
                pixelData[i + 3] = blue;
            }

            offset += mipSize;
        }
    }

    public static void A8B8G8R8toB8G8R8A8(byte[] pixelData, int width, int height, int mipmapCount)
    {
        int offset = 0;
        for (int mip = 0; mip < mipmapCount; mip++)
        {
            int mipWidth = Math.Max(1, width >> mip);
            int mipHeight = Math.Max(1, height >> mip);
            int mipSize = mipWidth * mipHeight * 4;

            for (int i = offset; i < offset + mipSize; i += 4)
            {
                byte alpha = pixelData[i];
                byte blue = pixelData[i + 1];
                byte green = pixelData[i + 2];
                byte red = pixelData[i + 3];

                pixelData[i] = blue;
                pixelData[i + 1] = green;
                pixelData[i + 2] = red;
                pixelData[i + 3] = alpha;
            }

            offset += mipSize;
        }
    }

    private static DDSWriteConfiguration CreateWriteConfiguration(TextureBase texture, byte[] bitmapData)
    {
        return texture switch
        {
            TexturePC pc => CreateWriteConfiguration(pc, bitmapData),
            TextureBPR bpr => CreateWriteConfiguration(bpr),
            TexturePS3 ps3 => CreateWriteConfiguration(ps3, bitmapData),
            TextureX360 x360 => CreateWriteConfiguration(x360, bitmapData),
            _ => throw new NotSupportedException($"DDS export is not supported for texture type '{texture.GetType().Name}'."),
        };
    }

    private static DDSWriteConfiguration CreateWriteConfiguration(TexturePC texture, byte[] bitmapData)
    {
        return texture.Format switch
        {
            D3DFORMAT.D3DFMT_DXT1 => DDSWriteConfiguration.Compressed(FourCC("DXT1"), 8),
            D3DFORMAT.D3DFMT_DXT3 => DDSWriteConfiguration.Compressed(FourCC("DXT3"), 16),
            D3DFORMAT.D3DFMT_DXT5 => DDSWriteConfiguration.Compressed(FourCC("DXT5"), 16),
            D3DFORMAT.D3DFMT_A8 => DDSWriteConfiguration.Alpha8(),
            D3DFORMAT.D3DFMT_A8R8G8B8 => ConvertArgbToDdsBgra(bitmapData, texture.Width, texture.Height, texture.MipmapLevels),
            D3DFORMAT.D3DFMT_A8B8G8R8 => ConvertAbgrToDdsBgra(bitmapData, texture.Width, texture.Height, texture.MipmapLevels),
            D3DFORMAT.D3DFMT_R5G6B5 => DDSWriteConfiguration.Rgb(16, 0xF800u, 0x07E0u, 0x001Fu, 0u),
            D3DFORMAT.D3DFMT_A1R5G5B5 => DDSWriteConfiguration.RgbAlpha(16, 0x7C00u, 0x03E0u, 0x001Fu, 0x8000u),
            D3DFORMAT.D3DFMT_A4R4G4B4 => DDSWriteConfiguration.RgbAlpha(16, 0x0F00u, 0x00F0u, 0x000Fu, 0xF000u),
            _ => throw new NotSupportedException($"DDS export is not supported for TUB texture format '{texture.Format}'."),
        };
    }

    private static DDSWriteConfiguration CreateWriteConfiguration(TextureBPR texture)
    {
        return texture.Format switch
        {
            DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM or DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM_SRGB
                => DDSWriteConfiguration.Compressed(FourCC("DXT1"), 8),
            DXGI_FORMAT.DXGI_FORMAT_BC2_UNORM or DXGI_FORMAT.DXGI_FORMAT_BC2_UNORM_SRGB
                => DDSWriteConfiguration.Compressed(FourCC("DXT3"), 16),
            DXGI_FORMAT.DXGI_FORMAT_BC3_UNORM or DXGI_FORMAT.DXGI_FORMAT_BC3_UNORM_SRGB
                => DDSWriteConfiguration.Compressed(FourCC("DXT5"), 16),
            DXGI_FORMAT.DXGI_FORMAT_A8_UNORM => DDSWriteConfiguration.Alpha8(),
            DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM or DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM_SRGB
                => DDSWriteConfiguration.Bgra8(),
            DXGI_FORMAT.DXGI_FORMAT_B8G8R8X8_UNORM or DXGI_FORMAT.DXGI_FORMAT_B8G8R8X8_UNORM_SRGB
                => DDSWriteConfiguration.Rgb(32, 0x00FF0000u, 0x0000FF00u, 0x000000FFu, 0u),
            DXGI_FORMAT.DXGI_FORMAT_B5G6R5_UNORM
                => DDSWriteConfiguration.Rgb(16, 0xF800u, 0x07E0u, 0x001Fu, 0u),
            DXGI_FORMAT.DXGI_FORMAT_B5G5R5A1_UNORM
                => DDSWriteConfiguration.RgbAlpha(16, 0x7C00u, 0x03E0u, 0x001Fu, 0x8000u),
            DXGI_FORMAT.DXGI_FORMAT_B4G4R4A4_UNORM
                => DDSWriteConfiguration.RgbAlpha(16, 0x0F00u, 0x00F0u, 0x000Fu, 0xF000u),
            DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM or DXGI_FORMAT.DXGI_FORMAT_R16G16B16A16_UNORM
                => DDSWriteConfiguration.Dx10(texture.Format),
            _ => throw new NotSupportedException($"DDS export is not supported for BPR texture format '{texture.Format}'."),
        };
    }

    private static DDSWriteConfiguration CreateWriteConfiguration(TexturePS3 texture, byte[] bitmapData)
    {
        return texture.Format switch
        {
            CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_COMPRESSED_DXT1 => DDSWriteConfiguration.Compressed(FourCC("DXT1"), 8),
            CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_COMPRESSED_DXT23 => DDSWriteConfiguration.Compressed(FourCC("DXT3"), 16),
            CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_COMPRESSED_DXT45 => DDSWriteConfiguration.Compressed(FourCC("DXT5"), 16),
            CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_B8 => DDSWriteConfiguration.Alpha8(),
            CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_A8R8G8B8 => ConvertArgbToDdsBgra(bitmapData, texture.Width, texture.Height, texture.MipmapLevels),
            _ => throw new NotSupportedException($"DDS export is not supported for PS3 texture format '{texture.Format}'."),
        };
    }

    private static DDSWriteConfiguration CreateWriteConfiguration(TextureX360 texture, byte[] bitmapData)
    {
        return texture.Format.DataFormat switch
        {
            GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT1 => DDSWriteConfiguration.Compressed(FourCC("DXT1"), 8),
            GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT2_3 => DDSWriteConfiguration.Compressed(FourCC("DXT3"), 16),
            GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT4_5 => DDSWriteConfiguration.Compressed(FourCC("DXT5"), 16),
            GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_8 => DDSWriteConfiguration.Alpha8(),
            GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_8_8_8_8 => ConvertArgbToDdsBgra(bitmapData, texture.Width, texture.Height, texture.MipmapLevels),
            GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_16_16_16_16 => DDSWriteConfiguration.Dx10(DXGI_FORMAT.DXGI_FORMAT_R16G16B16A16_UNORM),
            _ => throw new NotSupportedException($"DDS export is not supported for X360 texture format '{texture.Format.DataFormat}'."),
        };
    }

    private static DDSWriteConfiguration ConvertArgbToDdsBgra(byte[] bitmapData, int width, int height, int mipmapCount)
    {
        A8R8G8B8toB8G8R8A8(bitmapData, width, height, Math.Max(1, mipmapCount));
        return DDSWriteConfiguration.Bgra8();
    }

    private static DDSWriteConfiguration ConvertAbgrToDdsBgra(byte[] bitmapData, int width, int height, int mipmapCount)
    {
        A8B8G8R8toB8G8R8A8(bitmapData, width, height, Math.Max(1, mipmapCount));
        return DDSWriteConfiguration.Bgra8();
    }

    private static uint GetHeaderFlags(TextureBase texture, DDSWriteConfiguration configuration)
    {
        const uint DDSD_CAPS = 0x1;
        const uint DDSD_HEIGHT = 0x2;
        const uint DDSD_WIDTH = 0x4;
        const uint DDSD_PITCH = 0x8;
        const uint DDSD_PIXELFORMAT = 0x1000;
        const uint DDSD_MIPMAPCOUNT = 0x20000;
        const uint DDSD_LINEARSIZE = 0x80000;
        const uint DDSD_DEPTH = 0x800000;

        uint flags = DDSD_CAPS | DDSD_HEIGHT | DDSD_WIDTH | DDSD_PIXELFORMAT;
        flags |= configuration.IsCompressed ? DDSD_LINEARSIZE : DDSD_PITCH;

        if (Math.Max(1, (int)texture.MipmapLevels) > 1)
        {
            flags |= DDSD_MIPMAPCOUNT;
        }

        if (texture.Dimension == DIMENSION.DIMENSION_3D)
        {
            flags |= DDSD_DEPTH;
        }

        return flags;
    }

    private static uint GetCaps(TextureBase texture)
    {
        const uint DDSCAPS_COMPLEX = 0x8;
        const uint DDSCAPS_TEXTURE = 0x1000;
        const uint DDSCAPS_MIPMAP = 0x400000;

        uint caps = DDSCAPS_TEXTURE;

        if (Math.Max(1, (int)texture.MipmapLevels) > 1)
        {
            caps |= DDSCAPS_COMPLEX | DDSCAPS_MIPMAP;
        }

        if (texture.Dimension is DIMENSION.DIMENSION_CUBE or DIMENSION.DIMENSION_3D)
        {
            caps |= DDSCAPS_COMPLEX;
        }

        return caps;
    }

    private static uint GetCaps2(TextureBase texture)
    {
        const uint DDSCAPS2_CUBEMAP = 0x200;
        const uint DDSCAPS2_CUBEMAP_ALLFACES = 0xFC00;
        const uint DDSCAPS2_VOLUME = 0x200000;

        return texture.Dimension switch
        {
            DIMENSION.DIMENSION_CUBE => DDSCAPS2_CUBEMAP | DDSCAPS2_CUBEMAP_ALLFACES,
            DIMENSION.DIMENSION_3D => DDSCAPS2_VOLUME,
            _ => 0u,
        };
    }

    private static uint GetArraySize(TextureBase texture)
    {
        return texture is TextureBPR bpr ? Math.Max(1u, (uint)bpr.ArraySize) : 1u;
    }

    private static D3D10ResourceDimension GetResourceDimension(DIMENSION dimension)
    {
        return dimension switch
        {
            DIMENSION.DIMENSION_1D => D3D10ResourceDimension.Texture1D,
            DIMENSION.DIMENSION_3D => D3D10ResourceDimension.Texture3D,
            _ => D3D10ResourceDimension.Texture2D,
        };
    }

    private static void WritePixelFormat(BinaryWriter writer, DDSWriteConfiguration configuration)
    {
        writer.Write(32u);
        writer.Write(configuration.PixelFormatFlags);
        writer.Write(configuration.FourCC);
        writer.Write(configuration.RgbBitCount);
        writer.Write(configuration.RBitMask);
        writer.Write(configuration.GBitMask);
        writer.Write(configuration.BBitMask);
        writer.Write(configuration.ABitMask);
    }

    private static uint FourCC(string value)
    {
        return (uint)value[0]
             | ((uint)value[1] << 8)
             | ((uint)value[2] << 16)
             | ((uint)value[3] << 24);
    }

    private sealed class DDSWriteConfiguration
    {
        public required uint PixelFormatFlags { get; init; }
        public required uint FourCC { get; init; }
        public required uint RgbBitCount { get; init; }
        public required uint RBitMask { get; init; }
        public required uint GBitMask { get; init; }
        public required uint BBitMask { get; init; }
        public required uint ABitMask { get; init; }
        public required bool IsCompressed { get; init; }
        public required int BytesPerBlockOrPixel { get; init; }
        public DXGI_FORMAT DxgiFormat { get; init; } = DXGI_FORMAT.DXGI_FORMAT_UNKNOWN;
        public bool UseDx10Header { get; init; }

        public uint GetPitchOrLinearSize(int width)
        {
            if (IsCompressed)
            {
                return (uint)(Math.Max(1, (width + 3) / 4) * BytesPerBlockOrPixel);
            }

            return (uint)(width * BytesPerBlockOrPixel);
        }

        public static DDSWriteConfiguration Compressed(uint fourCC, int bytesPerBlock)
        {
            return new DDSWriteConfiguration
            {
                PixelFormatFlags = 0x4,
                FourCC = fourCC,
                RgbBitCount = 0,
                RBitMask = 0,
                GBitMask = 0,
                BBitMask = 0,
                ABitMask = 0,
                IsCompressed = true,
                BytesPerBlockOrPixel = bytesPerBlock,
            };
        }

        public static DDSWriteConfiguration Alpha8()
        {
            return new DDSWriteConfiguration
            {
                PixelFormatFlags = 0x2,
                FourCC = 0,
                RgbBitCount = 8,
                RBitMask = 0,
                GBitMask = 0,
                BBitMask = 0,
                ABitMask = 0xFF,
                IsCompressed = false,
                BytesPerBlockOrPixel = 1,
            };
        }

        public static DDSWriteConfiguration Bgra8()
        {
            return RgbAlpha(32, 0x00FF0000u, 0x0000FF00u, 0x000000FFu, 0xFF000000u);
        }

        public static DDSWriteConfiguration Rgb(uint bitCount, uint rMask, uint gMask, uint bMask, uint aMask)
        {
            return new DDSWriteConfiguration
            {
                PixelFormatFlags = 0x40,
                FourCC = 0,
                RgbBitCount = bitCount,
                RBitMask = rMask,
                GBitMask = gMask,
                BBitMask = bMask,
                ABitMask = aMask,
                IsCompressed = false,
                BytesPerBlockOrPixel = (int)(bitCount / 8),
            };
        }

        public static DDSWriteConfiguration RgbAlpha(uint bitCount, uint rMask, uint gMask, uint bMask, uint aMask)
        {
            return new DDSWriteConfiguration
            {
                PixelFormatFlags = 0x41,
                FourCC = 0,
                RgbBitCount = bitCount,
                RBitMask = rMask,
                GBitMask = gMask,
                BBitMask = bMask,
                ABitMask = aMask,
                IsCompressed = false,
                BytesPerBlockOrPixel = (int)(bitCount / 8),
            };
        }

        public static DDSWriteConfiguration Dx10(DXGI_FORMAT format)
        {
            return new DDSWriteConfiguration
            {
                PixelFormatFlags = 0x4,
                FourCC = DDSTextureUtilities.FourCC("DX10"),
                RgbBitCount = 0,
                RBitMask = 0,
                GBitMask = 0,
                BBitMask = 0,
                ABitMask = 0,
                IsCompressed = format is DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM
                    or DXGI_FORMAT.DXGI_FORMAT_BC2_UNORM
                    or DXGI_FORMAT.DXGI_FORMAT_BC3_UNORM,
                BytesPerBlockOrPixel = format switch
                {
                    DXGI_FORMAT.DXGI_FORMAT_R16G16B16A16_UNORM => 8,
                    DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM => 4,
                    _ => 4,
                },
                DxgiFormat = format,
                UseDx10Header = true,
            };
        }
    }

    private enum D3D10ResourceDimension : uint
    {
        Texture1D = 2,
        Texture2D = 3,
        Texture3D = 4,
    }
}
