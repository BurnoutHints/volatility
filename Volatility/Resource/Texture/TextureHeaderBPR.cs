using static Volatility.Utilities.DataUtilities;

namespace Volatility.Resource.Texture;

public class TextureHeaderBPR : TextureHeaderBase
{
    public override Endian GetResourceEndian() => Endian.LE;

    public bool x64Header;                                      // For platforms like PS4

    public D3D11_USAGE Usage = D3D11_USAGE.D3D11_USAGE_DEFAULT; // Usually default, implemented for parity sake
    public DXGI_FORMAT Format;                                  // Format
    public byte[] Flags = new byte[4];                          // Unknown flags, 0
    public ushort ArraySize = 1;                                // Generally 1, likely for stacked textures
    public byte MostDetailedMip = 0;                            // The highest detailed mip to use
    public uint ArrayIndex = 377024;                            // Doc says < 32 but it's always 377024 (C0 C0 05 00)?
    public uint ContentsSize;                                   // PS4/Switch specific field, TODO: Calculate this

    public override DIMENSION Dimension
    {
        get => _Dimension;
        set => _Dimension = value;
    }

    public TextureHeaderBPR() : base() { }

    public TextureHeaderBPR(string path) : base(path) 
    {
        // string texturePath = $"{path}_texture.dat";
        // 
        // if (!File.Exists(texturePath))
        //     return;
        // 
        // ContentsSize = (uint)new FileInfo(texturePath).Length;
    }

    public override void PushInternalFormat() { }
    public override void PullInternalFormat() { }
    public override void PushInternalFlags() { }
    public override void PullInternalFlags() => base.PullInternalFlags();

    public override void WriteToStream(EndianAwareBinaryWriter writer)
    {
        base.WriteToStream(writer);

        writer.Write(x64Switch(x64Header, 0));  // TextureInterfacePtr, 64 bit
        writer.Write((uint)Usage);
        writer.Write((uint)Dimension);
        writer.Write(x64Switch(x64Header, 0));  // PixelDataPtr, 64 bit
        writer.Write(x64Switch(x64Header, 0));  // ShaderResourceViewInterface0Ptr, 64 bit
        writer.Write(x64Switch(x64Header, 0));  // ShaderResourceViewInterface1Ptr, 64 bit
        writer.Write((uint)0);                  // Unknown0
        writer.Write((uint)Format);
        writer.Write(Flags);
        writer.Write(Width);
        writer.Write(Height);
        writer.Write(Depth);
        writer.Write(ArraySize);
        writer.Write(MostDetailedMip);
        writer.Write(MipmapLevels);
        writer.Write((ushort)0);                // Unknown1
        writer.Write(x64Switch(x64Header, 0));  // Unknown2, 64 bit
        writer.Write(ArrayIndex);
        writer.Write(ContentsSize);
        writer.Write(x64Switch(x64Header, 0));  // TextureData, 64 bit
    }

    public override void ParseFromStream(EndianAwareBinaryReader reader)
    {
        base.ParseFromStream(reader);

        x64Header = reader.BaseStream.Length > 0x40;

        reader.BaseStream.Seek(x64Header ? 0x8 : 0x4, SeekOrigin.Begin);    // Skip TextureInterfacePtr
        Usage = (D3D11_USAGE)reader.ReadInt32();
        Dimension = (DIMENSION)reader.ReadInt32();
        reader.BaseStream.Seek(x64Header ? 0x18 : 0xC, SeekOrigin.Current); // Skip pointers
        reader.BaseStream.Seek(0x4, SeekOrigin.Current);                    // Skip Unknown0
        Format = (DXGI_FORMAT)reader.ReadInt32();
        reader.Read(Flags);
        Width = reader.ReadUInt16();
        Height = reader.ReadUInt16();
        Depth = reader.ReadUInt16();
        ArraySize = reader.ReadUInt16();
        MostDetailedMip = reader.ReadByte();
        MipmapLevels = reader.ReadByte();
        reader.BaseStream.Seek(sizeof(ushort), SeekOrigin.Current);         // Skip Unknown1
        reader.BaseStream.Seek(x64Header ? 0x8 : 0x4, SeekOrigin.Current);  // Unknown 2, 64 bit
        ArrayIndex = (uint)reader.ReadInt32();
        ContentsSize = (uint)reader.ReadInt32();
        reader.BaseStream.Seek(x64Header ? 0x8 : 0x4, SeekOrigin.Current);  // TextureData, 64 bit
    }

    public override void PushInternalDimension()
    {
        // Not needed for BPR; base dimension is BPR formatted
    }

    public override void PullInternalDimension()
    {
        // Not needed for BPR; base dimension is BPR formatted
    }
}

public enum D3D11_USAGE : int   // 32 bit value
{
    D3D11_USAGE_DEFAULT = 0,
    D3D11_USAGE_IMMUTABLE = 1,
    D3D11_USAGE_DYNAMIC = 2,
    D3D11_USAGE_STAGING = 3
}

public enum DXGI_FORMAT : int   // 32 bit value
{
    DXGI_FORMAT_UNKNOWN = 0,
    DXGI_FORMAT_R32G32B32A32_TYPELESS = 1,
    DXGI_FORMAT_R32G32B32A32_FLOAT = 2,
    DXGI_FORMAT_R32G32B32A32_UINT = 3,
    DXGI_FORMAT_R32G32B32A32_SINT = 4,
    DXGI_FORMAT_R32G32B32_TYPELESS = 5,
    DXGI_FORMAT_R32G32B32_FLOAT = 6,
    DXGI_FORMAT_R32G32B32_UINT = 7,
    DXGI_FORMAT_R32G32B32_SINT = 8,
    DXGI_FORMAT_R16G16B16A16_TYPELESS = 9,
    DXGI_FORMAT_R16G16B16A16_FLOAT = 10,
    DXGI_FORMAT_R16G16B16A16_UNORM = 11,
    DXGI_FORMAT_R16G16B16A16_UINT = 12,
    DXGI_FORMAT_R16G16B16A16_SNORM = 13,
    DXGI_FORMAT_R16G16B16A16_SINT = 14,
    DXGI_FORMAT_R32G32_TYPELESS = 15,
    DXGI_FORMAT_R32G32_FLOAT = 16,
    DXGI_FORMAT_R32G32_UINT = 17,
    DXGI_FORMAT_R32G32_SINT = 18,
    DXGI_FORMAT_R32G8X24_TYPELESS = 19,
    DXGI_FORMAT_D32_FLOAT_S8X24_UINT = 20,
    DXGI_FORMAT_R32_FLOAT_X8X24_TYPELESS = 21,
    DXGI_FORMAT_X32_TYPELESS_G8X24_UINT = 22,
    DXGI_FORMAT_R10G10B10A2_TYPELESS = 23,
    DXGI_FORMAT_R10G10B10A2_UNORM = 24,
    DXGI_FORMAT_R10G10B10A2_UINT = 25,
    DXGI_FORMAT_R11G11B10_FLOAT = 26,
    DXGI_FORMAT_R8G8B8A8_TYPELESS = 27,
    DXGI_FORMAT_R8G8B8A8_UNORM = 28,
    DXGI_FORMAT_R8G8B8A8_UNORM_SRGB = 29,
    DXGI_FORMAT_R8G8B8A8_UINT = 30,
    DXGI_FORMAT_R8G8B8A8_SNORM = 31,
    DXGI_FORMAT_R8G8B8A8_SINT = 32,
    DXGI_FORMAT_R16G16_TYPELESS = 33,
    DXGI_FORMAT_R16G16_FLOAT = 34,
    DXGI_FORMAT_R16G16_UNORM = 35,
    DXGI_FORMAT_R16G16_UINT = 36,
    DXGI_FORMAT_R16G16_SNORM = 37,
    DXGI_FORMAT_R16G16_SINT = 38,
    DXGI_FORMAT_R32_TYPELESS = 39,
    DXGI_FORMAT_D32_FLOAT = 40,
    DXGI_FORMAT_R32_FLOAT = 41,
    DXGI_FORMAT_R32_UINT = 42,
    DXGI_FORMAT_R32_SINT = 43,
    DXGI_FORMAT_R24G8_TYPELESS = 44,
    DXGI_FORMAT_D24_UNORM_S8_UINT = 45,
    DXGI_FORMAT_R24_UNORM_X8_TYPELESS = 46,
    DXGI_FORMAT_X24_TYPELESS_G8_UINT = 47,
    DXGI_FORMAT_R8G8_TYPELESS = 48,
    DXGI_FORMAT_R8G8_UNORM = 49,
    DXGI_FORMAT_R8G8_UINT = 50,
    DXGI_FORMAT_R8G8_SNORM = 51,
    DXGI_FORMAT_R8G8_SINT = 52,
    DXGI_FORMAT_R16_TYPELESS = 53,
    DXGI_FORMAT_R16_FLOAT = 54,
    DXGI_FORMAT_D16_UNORM = 55,
    DXGI_FORMAT_R16_UNORM = 56,
    DXGI_FORMAT_R16_UINT = 57,
    DXGI_FORMAT_R16_SNORM = 58,
    DXGI_FORMAT_R16_SINT = 59,
    DXGI_FORMAT_R8_TYPELESS = 60,
    DXGI_FORMAT_R8_UNORM = 61,
    DXGI_FORMAT_R8_UINT = 62,
    DXGI_FORMAT_R8_SNORM = 63,
    DXGI_FORMAT_R8_SINT = 64,
    DXGI_FORMAT_A8_UNORM = 65,
    DXGI_FORMAT_R1_UNORM = 66,
    DXGI_FORMAT_R9G9B9E5_SHAREDEXP = 67,
    DXGI_FORMAT_R8G8_B8G8_UNORM = 68,
    DXGI_FORMAT_G8R8_G8B8_UNORM = 69,
    DXGI_FORMAT_BC1_TYPELESS = 70,
    DXGI_FORMAT_BC1_UNORM = 71,
    DXGI_FORMAT_BC1_UNORM_SRGB = 72,
    DXGI_FORMAT_BC2_TYPELESS = 73,
    DXGI_FORMAT_BC2_UNORM = 74,
    DXGI_FORMAT_BC2_UNORM_SRGB = 75,
    DXGI_FORMAT_BC3_TYPELESS = 76,
    DXGI_FORMAT_BC3_UNORM = 77,
    DXGI_FORMAT_BC3_UNORM_SRGB = 78,
    DXGI_FORMAT_BC4_TYPELESS = 79,
    DXGI_FORMAT_BC4_UNORM = 80,
    DXGI_FORMAT_BC4_SNORM = 81,
    DXGI_FORMAT_BC5_TYPELESS = 82,
    DXGI_FORMAT_BC5_UNORM = 83,
    DXGI_FORMAT_BC5_SNORM = 84,
    DXGI_FORMAT_B5G6R5_UNORM = 85,
    DXGI_FORMAT_B5G5R5A1_UNORM = 86,
    DXGI_FORMAT_B8G8R8A8_UNORM = 87,
    DXGI_FORMAT_B8G8R8X8_UNORM = 88,
    DXGI_FORMAT_R10G10B10_XR_BIAS_A2_UNORM = 89,
    DXGI_FORMAT_B8G8R8A8_TYPELESS = 90,
    DXGI_FORMAT_B8G8R8A8_UNORM_SRGB = 91,
    DXGI_FORMAT_B8G8R8X8_TYPELESS = 92,
    DXGI_FORMAT_B8G8R8X8_UNORM_SRGB = 93,
    DXGI_FORMAT_BC6H_TYPELESS = 94,
    DXGI_FORMAT_BC6H_UF16 = 95,
    DXGI_FORMAT_BC6H_SF16 = 96,
    DXGI_FORMAT_BC7_TYPELESS = 97,
    DXGI_FORMAT_BC7_UNORM = 98,
    DXGI_FORMAT_BC7_UNORM_SRGB = 99,
    DXGI_FORMAT_AYUV = 100,
    DXGI_FORMAT_Y410 = 101,
    DXGI_FORMAT_Y416 = 102,
    DXGI_FORMAT_NV12 = 103,
    DXGI_FORMAT_P010 = 104,
    DXGI_FORMAT_P016 = 105,
    DXGI_FORMAT_420_OPAQUE = 106,
    DXGI_FORMAT_YUY2 = 107,
    DXGI_FORMAT_Y210 = 108,
    DXGI_FORMAT_Y216 = 109,
    DXGI_FORMAT_NV11 = 110,
    DXGI_FORMAT_AI44 = 111,
    DXGI_FORMAT_IA44 = 112,
    DXGI_FORMAT_P8 = 113,
    DXGI_FORMAT_A8P8 = 114,
    DXGI_FORMAT_B4G4R4A4_UNORM = 115,
    DXGI_FORMAT_P208 = 130,
    DXGI_FORMAT_V208 = 131,
    DXGI_FORMAT_V408 = 132,
    DXGI_FORMAT_SAMPLER_FEEDBACK_MIN_MIP_OPAQUE = 133,
    DXGI_FORMAT_SAMPLER_FEEDBACK_MIP_REGION_USED_OPAQUE = 134
};
