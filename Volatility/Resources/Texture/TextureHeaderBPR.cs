using static Volatility.Utilities.DataUtilities;

namespace Volatility.Resources;

public class TextureHeaderBPR : TextureHeaderBase
{
    public override Endian GetResourceEndian() => Endian.LE;
    public override Platform GetResourcePlatform() => Platform.BPR;

    public D3D11_USAGE Usage = D3D11_USAGE.D3D11_USAGE_DEFAULT;         // Usually default, implemented for parity sake

    [EditorCategory("Texture/Remastered"), EditorLabel("Format"), EditorTooltip("The DXGI format of the texture.")]
    public DXGI_FORMAT Format;                                          // Format

    [EditorCategory("Texture/Remastered"), EditorLabel("Flags"), EditorTooltip("Flags that are primarily used on console platforms. \"Placed texture\" is unsupported on PC.")]
    public BPRTextureFlags Flags;                                       // Somewhat unknown flags, 0 on PC

    [EditorCategory("Texture/Remastered"), EditorLabel("Texture Array Size"), EditorTooltip("When using the stacked texture mode, specifies the amount of texture in the array. Use \"1\" otherwise.")]
    public ushort ArraySize = 1;                                        // Generally 1, likely for stacked textures
    
    [EditorCategory("Texture/Remastered/Placed Texture"), EditorLabel("Tile Mode"), EditorTooltip("When placed texture mode is enabled, this specifies the way the texture is tiled.")]
    public XG_TILE_MODE PlacedTileMode = XG_TILE_MODE.XG_TILE_MODE_PC;  // PC uses unknown value 0x0005C0C0, labeled "XG_TILE_MODE_PC" for now

    [EditorCategory("Texture/Remastered/Placed Texture"), EditorLabel("Texture Data Size"), EditorTooltip("When placed texture mode is enabled, this specifies the size of the texture data.")]
    public uint PlacedDataSize;                                         // TODO: Calculate this PS4/Switch/XBOne specific field

    public override DIMENSION Dimension
    {
        get => _Dimension;
        set => _Dimension = value;
    }

    public TextureHeaderBPR() : base() { }

    public TextureHeaderBPR(string path, Endian endianness = Endian.Agnostic) : base(path, endianness) { }

    public override void PushInternalFormat() { }
    public override void PullInternalFormat() { }
    public override void PushInternalFlags() { }
    public override void PullInternalFlags() => base.PullInternalFlags();

    public override void WriteToStream(EndianAwareBinaryWriter writer, Endian endianness = Endian.Agnostic)
    {
        base.WriteToStream(writer, endianness);

        writer.Write(x64Switch(GetResourceArch() == Arch.x64, 0));  // TextureInterfacePtr, 64 bit
        writer.Write((uint)Usage);
        writer.Write((uint)Dimension);
        writer.Write(x64Switch(GetResourceArch() == Arch.x64, 0));  // PixelDataPtr, 64 bit
        writer.Write(x64Switch(GetResourceArch() == Arch.x64, 0));  // ShaderResourceViewInterface0Ptr, 64 bit
        writer.Write(x64Switch(GetResourceArch() == Arch.x64, 0));  // ShaderResourceViewInterface1Ptr, 64 bit
        writer.Write((uint)0);                  // Unknown0
        writer.Write((uint)Format);
        writer.Write((uint)Flags);
        writer.Write(Width);
        writer.Write(Height);
        writer.Write(Depth);
        writer.Write(ArraySize);
        writer.Write(MostDetailedMip);
        writer.Write(MipmapLevels);
        writer.Write((ushort)0);                // Unknown1
        writer.Write(x64Switch(GetResourceArch() == Arch.x64, 0));  // Unknown2, 64 bit
        writer.Write((int)PlacedTileMode);
        writer.Write(PlacedDataSize);
        writer.Write(x64Switch(GetResourceArch() == Arch.x64, 0));  // TextureData, 64 bit

        if (GetResourceArch() == Arch.x64)
        {
            writer.Write(System.Text.Encoding.ASCII.GetBytes("Volatili"));
        }
    }

    public override void ParseFromStream(ResourceBinaryReader reader, Endian endianness = Endian.Agnostic)
    {
        base.ParseFromStream(reader, endianness);

        SetResourceArch(reader.BaseStream.Length > 0x40 ? Arch.x64 : Arch.x32);

        reader.BaseStream.Seek(GetResourceArch() == Arch.x64 ? 0x8 : 0x4, SeekOrigin.Begin);    // Skip TextureInterfacePtr
        Usage = (D3D11_USAGE)reader.ReadInt32();
        Dimension = (DIMENSION)reader.ReadInt32();
        reader.BaseStream.Seek(GetResourceArch() == Arch.x64 ? 0x18 : 0xC, SeekOrigin.Current); // Skip pointers
        reader.BaseStream.Seek(0x4, SeekOrigin.Current);                    // Skip Unknown0
        Format = (DXGI_FORMAT)reader.ReadInt32();
        Flags = (BPRTextureFlags)reader.ReadUInt32();
        Width = reader.ReadUInt16();
        Height = reader.ReadUInt16();
        Depth = reader.ReadUInt16();
        ArraySize = reader.ReadUInt16();
        MostDetailedMip = reader.ReadByte();
        MipmapLevels = reader.ReadByte();
        reader.BaseStream.Seek(sizeof(ushort), SeekOrigin.Current);         // Skip Unknown1
        reader.BaseStream.Seek(GetResourceArch() == Arch.x64 ? 0x8 : 0x4, SeekOrigin.Current);  // Unknown 2, 64 bit
        PlacedTileMode = (XG_TILE_MODE)reader.ReadInt32();
        PlacedDataSize = (uint)reader.ReadInt32();
        reader.BaseStream.Seek(GetResourceArch() == Arch.x64 ? 0x8 : 0x4, SeekOrigin.Current);  // TextureData, 64 bit
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
public enum XG_TILE_MODE : int
{
    XG_TILE_MODE_INVALID = -1,
    XG_TILE_MODE_COMP_DEPTH_0 = 0,
    XG_TILE_MODE_COMP_DEPTH_1 = 1,
    XG_TILE_MODE_COMP_DEPTH_2 = 2,
    XG_TILE_MODE_COMP_DEPTH_3 = 3,
    XG_TILE_MODE_COMP_DEPTH_4 = 4,
    XG_TILE_MODE_UNC_DEPTH_5 = 5,
    XG_TILE_MODE_UNC_DEPTH_6 = 6,
    XG_TILE_MODE_UNC_DEPTH_7 = 7,
    XG_TILE_MODE_LINEAR = 8,
    XG_TILE_MODE_DISPLAY = 9,
    XG_TILE_MODE_2D_DISPLAY = 10,
    XG_TILE_MODE_TILED_DISPLAY = 11,
    XG_TILE_MODE_TILED_2D_DISPLAY = 12,
    XG_TILE_MODE_1D_THIN = 13,  // 8x8x1
    XG_TILE_MODE_2D_THIN = 14,
    XG_TILE_MODE_3D_THIN = 15,
    XG_TILE_MODE_TILED_1D_THIN = 16,
    XG_TILE_MODE_TILED_2D_THIN = 17,
    XG_TILE_MODE_TILED_3D_THIN = 18,
    XG_TILE_MODE_1D_THICK = 19, // 4x4x4
    XG_TILE_MODE_2D_THICK = 20,
    XG_TILE_MODE_3D_THICK = 21,
    XG_TILE_MODE_TILED_1D_THICK = 22,
    XG_TILE_MODE_TILED_2D_THICK = 23,
    XG_TILE_MODE_TILED_3D_THICK = 24,
    XG_TILE_MODE_2D_XTHICK = 25,
    XG_TILE_MODE_3D_XTHICK = 26,
    XG_TILE_MODE_RESERVED_27 = 27,
    XG_TILE_MODE_RESERVED_28 = 28,
    XG_TILE_MODE_RESERVED_29 = 29,
    XG_TILE_MODE_RESERVED_30 = 30,
    XG_TILE_MODE_LINEAR_GENERAL = 31,
    XG_TILE_MODE_TILED_2D_DEPTH = XG_TILE_MODE_UNC_DEPTH_7,
    XG_TILE_MODE_PC = 0x5C0C0   // PC-specific unknown
}

[Flags]
public enum BPRTextureFlags : uint
{
    NoInitialPixelData = 0x1,
    RenderTarget = 0x2,
    CPUWriteAccess = 0x4,
    EnableMipGeneration = 0x8,
    Unknown0x10 = 0x10,
    Unknown0x20 = 0x20,
    DepthStencil = 0x40,
    Unknown0x80 = 0x80,
    Unknown0x100 = 0x100,
    PlacedTexture = 0x200,
    NoDepthCompression = 0x400000,
    NoColourCompression = 0x800000,
}
