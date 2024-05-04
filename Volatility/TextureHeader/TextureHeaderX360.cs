using System.Collections;
using Volatility.Utilities;

using static Volatility.Utilities.DataUtilities;

namespace Volatility.TextureHeader;

public class TextureHeaderX360 : TextureHeaderBase
{
    public BitArray D3DResourceFlags = new BitArray(28);
    public D3DRESOURCETYPE D3DRESOURCETYPE = D3DRESOURCETYPE.D3DRTYPE_TEXTURE;
    public uint ReferenceCount = 1;
    public uint Fence;
    public uint ReadFence;
    public uint Identifier;
    public uint BaseFlush = 65535;
    public uint MipFlush = 65535;
    public GPUTEXTURE_FETCH_CONSTANT Format;

    public TextureHeaderX360() : base() {}
    
    public TextureHeaderX360(string path) : base(path) {}
    
    public override void PullInternalDimension()
    {
        DIMENSION OutputDimension = Format.Dimension switch
        {
            GPUDIMENSION.GPUDIMENSION_1D => DIMENSION.DIMENSION_1D,
            GPUDIMENSION.GPUDIMENSION_3D => DIMENSION.DIMENSION_3D,
            GPUDIMENSION.GPUDIMENSION_CUBEMAP => DIMENSION.DIMENSION_CUBE,
            _ => DIMENSION.DIMENSION_2D,
        };
        // Directly set the internal field to not trigger a push
        // Is this good a good practice?
        _Dimension = OutputDimension;

        // Technically 1D textures can hold a width longer than 
        // a ushort, but the possibility of a Xbox 360 texture 
        // being more than 65535 px long is nearly impossible.
        Width = (ushort)Format.Size.Width;
        Height = (ushort)Format.Size.Height;
        Depth = (ushort)Format.Size.Depth;
        MipmapLevels = (byte)(Format.MaxMipLevel - Format.MinMipLevel + 1);
    }
    
    public override void PullInternalFlags()
    {
        // TODO: Implement better parser
        // Not accurate. Only prevents 3D & cubemaps from being GR/Prop Textures
        GRTexture = PropTexture = Format.Dimension switch
        {
            GPUDIMENSION.GPUDIMENSION_1D => true,
            GPUDIMENSION.GPUDIMENSION_3D => false,
            GPUDIMENSION.GPUDIMENSION_CUBEMAP => false,
            _ => true,
        };
        WorldTexture = true;

        base.PullInternalFlags();
    }

    public override void PullInternalFormat()
    {
        // Not needed for 360
    }
    
    public override void PushInternalDimension()
    {
        var OutputDimension = Dimension switch
        {
            DIMENSION.DIMENSION_1D => GPUDIMENSION.GPUDIMENSION_1D,
            DIMENSION.DIMENSION_3D => GPUDIMENSION.GPUDIMENSION_3D,
            DIMENSION.DIMENSION_CUBE => GPUDIMENSION.GPUDIMENSION_CUBEMAP,
            _ => GPUDIMENSION.GPUDIMENSION_2D,
        };
        Format.Dimension = OutputDimension;

        // No validation for now!!
        // TODO: Please add validation later
        Format.Size.Width = Width;
        Format.Size.Height = Height;
        Format.Size.Depth = Depth;
        Format.MaxMipLevel = (byte)(MipmapLevels - 1);
        Format.MinMipLevel = 0;
    }
    
    // parse GPUTEXTURE_FETCH_CONSTANT
    public override void PushInternalFlags() { }

    // Not sure if this is accurate
    public override void PushInternalFormat() => Format.Pitch = (ushort)(Format.Size.Width / 32); 

    public override void WriteToStream(BinaryWriter writer)
    {
        int Common = 0;
        for (int i = 0; i < D3DResourceFlags.Length; i++)
        {
            if (D3DResourceFlags[i])
            {
                Common |= 1 << i;
            }
        }

        Common <<= 4;
        Common |= (byte)D3DRESOURCETYPE & 0xF;

        writer.Write(SwapEndian(Common));   // Why is this MSB and the others are LSB?
        writer.Write(ReferenceCount);
        writer.Write(Fence);
        writer.Write(ReadFence);
        writer.Write(Identifier);
        writer.Write(BaseFlush);
        writer.Write(MipFlush);
        writer.Write(Format.PackToBytes());
        writer.Write(new byte[0xC]);        // Padding that's usually just garbage data.
    }

    public override void ParseFromStream(BinaryReader reader)
    {
        base.ParseFromStream(reader);

        // Common
        using (BitReader bitReader = new BitReader(reader.ReadBytes(4)))
        {
            D3DResourceFlags = bitReader.ReadBitsToBitArray(28);
            D3DRESOURCETYPE = (D3DRESOURCETYPE)bitReader.ReadBitsToUInt(4);
        }

        // UInt values
        reader.BaseStream.Seek(0x4, SeekOrigin.Begin);
        ReferenceCount = reader.ReadUInt32();
        Fence = reader.ReadUInt32();
        ReadFence = reader.ReadUInt32();
        Identifier = reader.ReadUInt32();
        BaseFlush = reader.ReadUInt32();
        MipFlush = reader.ReadUInt32();
        
        // Format
        reader.BaseStream.Seek(0x1C, SeekOrigin.Begin);
        Format = new GPUTEXTURE_FETCH_CONSTANT().FromPacked(reader.ReadBytes(0x18));
    }
}

public struct GPUTEXTURE_FETCH_CONSTANT 
{
    public bool Tiled;                        // 1 bit
    public ushort Pitch;                      // 9 bits + 1 bit padding
    public GPUMULTISAMPLE_TYPE MultiSample;   // 2 bits
    public GPUCLAMP ClampZ;                   // 3 bits
    public GPUCLAMP ClampY;                   // 3 bits
    public GPUCLAMP ClampX;                   // 3 bits
    public GPUSIGN SignW;                     // 2 bits
    public GPUSIGN SignZ;                     // 2 bits
    public GPUSIGN SignY;                     // 2 bits
    public GPUSIGN SignX;                     // 2 bits
    public GPUCONSTANTTYPE Type;              // 2 bits
    public uint BaseAddress;                  // 20 bits
    public GPUCLAMPPOLICY ClampPolicy;        // 1 bit
    public bool Stacked;                      // 1 bit
    public GPUREQUESTSIZE RequestSize;        // 2 bits
    public GPUENDIAN Endian;                  // 2 bits
    public GPUTEXTUREFORMAT DataFormat;       // 6 bits
    public GPUTEXTURESIZE Size;               // 32 bits, GPUTEXTURESIZE union
    public byte BorderSize;                   // 1 bit, 3 bit padding
    public GPUANISOFILTER AnisoFilter;        // 3 bits
    public GPUMIPFILTER MipFilter;            // 2 bits
    public GPUMINMAGFILTER MinFilter;         // 2 bits
    public GPUMINMAGFILTER MagFilter;         // 2 bits
    public byte ExpAdjust;                    // 6 bits
    public GPUSWIZZLE SwizzleW;               // 3 bits
    public GPUSWIZZLE SwizzleZ;               // 3 bits
    public GPUSWIZZLE SwizzleY;               // 3 bits
    public GPUSWIZZLE SwizzleX;               // 3 bits
    public GPUNUMFORMAT NumFormat;            // 1 bit
    public byte GradExpAdjustV;               // 5 bits
    public byte GradExpAdjustH;               // 5 bits
    public ushort LODBias;                    // 10 bits
    public bool MinAnisoWalk;                 // 1 bit
    public bool MagAnisoWalk;                 // 1 bit
    public byte MaxMipLevel;                  // 4 bits
    public byte MinMipLevel;                  // 4 bits
    public GPUMINMAGFILTER VolMinFilter;      // 1 bit
    public GPUMINMAGFILTER VolMagFilter;      // 1 bit
    public uint MipAddress;                   // 20 bits
    public bool PackedMips;                   // 1 bit
    public GPUDIMENSION Dimension;            // 2 bits
    public byte AnisoBias;                    // 4 bits
    public GPUTRICLAMP TriClamp;              // 2 bits
    public bool ForceBCWToMax;                // 1 bit
    public GPUBORDERCOLOR BorderColor;        // 2 bits

    public GPUTEXTURE_FETCH_CONSTANT FromPacked(byte[] bytes)
    {
        using BitReader bitReader = new BitReader(bytes);
        uint SizePacked;

        Tiled = bitReader.ReadBitsToUInt(1) != 0;
        Pitch = (ushort)bitReader.ReadBitsToUInt(9);
        _ = bitReader.ReadBitsToUInt(1);    // Padding
        MultiSample = (GPUMULTISAMPLE_TYPE)bitReader.ReadBitsToUInt(2);
        ClampZ = (GPUCLAMP)bitReader.ReadBitsToUInt(3);
        ClampY = (GPUCLAMP)bitReader.ReadBitsToUInt(3);
        ClampX = (GPUCLAMP)bitReader.ReadBitsToUInt(3);
        SignW = (GPUSIGN)bitReader.ReadBitsToUInt(2);
        SignZ = (GPUSIGN)bitReader.ReadBitsToUInt(2);
        SignY = (GPUSIGN)bitReader.ReadBitsToUInt(2);
        SignX = (GPUSIGN)bitReader.ReadBitsToUInt(2);
        Type = (GPUCONSTANTTYPE)bitReader.ReadBitsToUInt(2);
        BaseAddress = bitReader.ReadBitsToUInt(20);
        ClampPolicy = (GPUCLAMPPOLICY)bitReader.ReadBitsToUInt(1);
        Stacked = bitReader.ReadBitsToUInt(1) != 0;
        RequestSize = (GPUREQUESTSIZE)bitReader.ReadBitsToUInt(2);
        Endian = (GPUENDIAN)bitReader.ReadBitsToUInt(2);
        DataFormat = (GPUTEXTUREFORMAT)bitReader.ReadBitsToUInt(6);
        SizePacked = bitReader.ReadBitsToUInt(32);
        BorderSize = (byte)bitReader.ReadBitsToUInt(1);
        _ = bitReader.ReadBitsToUInt(3);    // Padding
        AnisoFilter = (GPUANISOFILTER)bitReader.ReadBitsToUInt(3);
        MipFilter = (GPUMIPFILTER)bitReader.ReadBitsToUInt(2);
        MinFilter = (GPUMINMAGFILTER)bitReader.ReadBitsToUInt(2);
        MagFilter = (GPUMINMAGFILTER)bitReader.ReadBitsToUInt(2);
        ExpAdjust = (byte)bitReader.ReadBitsToUInt(6);
        SwizzleW = (GPUSWIZZLE)bitReader.ReadBitsToUInt(3);
        SwizzleZ = (GPUSWIZZLE)bitReader.ReadBitsToUInt(3);
        SwizzleY = (GPUSWIZZLE)bitReader.ReadBitsToUInt(3);
        SwizzleX = (GPUSWIZZLE)bitReader.ReadBitsToUInt(3);
        NumFormat = (GPUNUMFORMAT)bitReader.ReadBitsToUInt(1);
        GradExpAdjustV = (byte)bitReader.ReadBitsToUInt(5);
        GradExpAdjustH = (byte)bitReader.ReadBitsToUInt(5);
        LODBias = (ushort)bitReader.ReadBitsToUInt(10);
        MinAnisoWalk = bitReader.ReadBitsToUInt(1) != 0;
        MagAnisoWalk = bitReader.ReadBitsToUInt(1) != 0;
        MaxMipLevel = (byte)bitReader.ReadBitsToUInt(4);
        MinMipLevel = (byte)bitReader.ReadBitsToUInt(4);
        VolMinFilter = (GPUMINMAGFILTER)bitReader.ReadBitsToUInt(1);
        VolMagFilter = (GPUMINMAGFILTER)bitReader.ReadBitsToUInt(1);
        MipAddress = bitReader.ReadBitsToUInt(20);
        PackedMips = bitReader.ReadBitsToUInt(1) != 0;
        Dimension = (GPUDIMENSION)bitReader.ReadBitsToUInt(2);
        AnisoBias = (byte)bitReader.ReadBitsToUInt(4);
        TriClamp = (GPUTRICLAMP)bitReader.ReadBitsToUInt(2);
        ForceBCWToMax = bitReader.ReadBitsToUInt(1) != 0;
        BorderColor = (GPUBORDERCOLOR)bitReader.ReadBitsToUInt(2);

        Size = new GPUTEXTURESIZE().FromPacked(SizePacked, Dimension);
        return this;
    }

    public byte[] PackToBytes()
    {
        BitWriter writer = new BitWriter(0x18);

        writer.Write(Tiled ? (uint)1 : 0, 1);
        writer.Write(Pitch, 9);
        writer.Write(0, 1); // Padding
        writer.Write((uint)MultiSample, 2);
        writer.Write((uint)ClampZ, 3);
        writer.Write((uint)ClampY, 3);
        writer.Write((uint)ClampX, 3);
        writer.Write((uint)SignW, 2);
        writer.Write((uint)SignZ, 2);
        writer.Write((uint)SignY, 2);
        writer.Write((uint)SignX, 2);
        writer.Write((uint)Type, 2);
        writer.Write(BaseAddress, 20);
        writer.Write((uint)ClampPolicy, 1);
        writer.Write(Stacked ? (uint)1 : 0, 1);
        writer.Write((uint)RequestSize, 2);
        writer.Write((uint)Endian, 2);
        writer.Write((uint)DataFormat, 6);
        writer.Write(Size.ToPacked(), 32);
        writer.Write(BorderSize, 1);
        writer.Write(0, 3); // Padding
        writer.Write((uint)AnisoFilter, 3);
        writer.Write((uint)MipFilter, 2);
        writer.Write((uint)MinFilter, 2);
        writer.Write((uint)MagFilter, 2);
        writer.Write(ExpAdjust, 6);
        writer.Write((uint)SwizzleW, 3);
        writer.Write((uint)SwizzleZ, 3);
        writer.Write((uint)SwizzleY, 3);
        writer.Write((uint)SwizzleX, 3);
        writer.Write((uint)NumFormat, 1);
        writer.Write(GradExpAdjustV, 5);
        writer.Write(GradExpAdjustH, 5);
        writer.Write(LODBias, 10);
        writer.Write(MinAnisoWalk ? (uint)1 : 0, 1);
        writer.Write(MagAnisoWalk ? (uint)1 : 0, 1);
        writer.Write(MaxMipLevel, 4);
        writer.Write(MinMipLevel, 4);
        writer.Write((uint)VolMinFilter, 1);
        writer.Write((uint)VolMagFilter, 1);
        writer.Write(MipAddress, 20);
        writer.Write(PackedMips ? (uint)1 : 0, 1);
        writer.Write((uint)Dimension, 2);
        writer.Write(AnisoBias, 4);
        writer.Write((uint)TriClamp, 2);
        writer.Write(ForceBCWToMax ? (uint)1 : 0, 1);
        writer.Write((uint)BorderColor, 2);

        return writer.ToArray();
    }
}

public struct GPUTEXTURESIZE
{
    public GPUTEXTURESIZE_TYPE Type;
    public uint Width;
    public uint Height;
    public uint Depth;

    public GPUTEXTURESIZE FromPacked(uint packed, GPUDIMENSION Dimension)
    {
        Width = Height = Depth = 1;
        Type = (GPUTEXTURESIZE_TYPE)Dimension; // May be inaccurate due to STACK
        switch (Type)
        {
            case GPUTEXTURESIZE_TYPE.GPUTEXTURESIZE_2D:
            case GPUTEXTURESIZE_TYPE.GPUTEXTURESIZE_STACK:
                Width += packed & 0x1FFF;
                Height += (packed >> 13) & 0x1FFF;
                Depth += Type == GPUTEXTURESIZE_TYPE.GPUTEXTURESIZE_STACK ? (packed >> 26) & 0x3F : 0;
                break;
            case GPUTEXTURESIZE_TYPE.GPUTEXTURESIZE_1D:
                Width += packed & 0xFFFFFF;
                break;
            case GPUTEXTURESIZE_TYPE.GPUTEXTURESIZE_3D:
                Width += packed & 0x7FF;
                Height += (packed >> 11) & 0x7FF;
                Depth += (packed >> 22) & 0x3FF; 
                break;
        }
        return this;
    }

    public uint ToPacked()
    {
        uint packed = 0;
        switch (Type)
        {
            case GPUTEXTURESIZE_TYPE.GPUTEXTURESIZE_1D:
                packed |= (Width - 1 & 0xFFFFFF);
                break;
            case GPUTEXTURESIZE_TYPE.GPUTEXTURESIZE_2D:
                packed |= Width - 1 & 0x1FFF;
                packed |= (Height - 1 & 0x1FFF) << 13;
                break;
            case GPUTEXTURESIZE_TYPE.GPUTEXTURESIZE_3D:
                packed |= Width - 1 & 0x7FF;
                packed |= (Height - 1 & 0x7FF) << 11;
                packed |= (Depth - 1 & 0x3FF) << 22;
                break;
            case GPUTEXTURESIZE_TYPE.GPUTEXTURESIZE_STACK:
                packed |= Width - 1 & 0x1FFF;
                packed |= (Height - 1 & 0x1FFF) << 13;
                packed |= (Depth - 1 & 0x3F) << 26;
                break;
        }
        return packed;
    }
}

public enum GPUTEXTURESIZE_TYPE : byte
{
    GPUTEXTURESIZE_1D = 0,
    GPUTEXTURESIZE_2D = 1,
    GPUTEXTURESIZE_3D = 2,
    GPUTEXTURESIZE_STACK = 3
}

public enum D3DRESOURCETYPE : byte      // 4 bit value
{
    D3DRTYPE_NONE = 0,
    D3DRTYPE_VERTEXBUFFER = 1,
    D3DRTYPE_INDEXBUFFER = 2,
    D3DRTYPE_TEXTURE = 3,
    D3DRTYPE_SURFACE = 4,
    D3DRTYPE_VERTEXDECLARATION = 5,
    D3DRTYPE_VERTEXSHADER = 6,
    D3DRTYPE_PIXELSHADER = 7,
    D3DRTYPE_CONSTANTBUFFER = 8,
    D3DRTYPE_COMMANDBUFFER = 9,
    D3DRTYPE_ASYNCCOMMANDBUFFERCALL1 = 10,
    D3DRTYPE_PERFCOUNTERBATCH1 = 11,
    D3DRTYPE_OCCLUSIONQUERYBATCH1 = 12,
    D3DRTYPE_VOLUME1 = 16,
    D3DRTYPE_VOLUMETEXTURE1 = 17,
    D3DRTYPE_CUBETEXTURE1 = 18,
    D3DRTYPE_ARRAYTEXTURE1 = 19,
    D3DRTYPE_LINETEXTURE2 = 20
    // D3DRTYPE_FORCE_DWORD = 0x7FFFFFFF // Unused
} 

public enum GPUMULTISAMPLE_TYPE : byte  // 2 bit value
{
    D3DMULTISAMPLE_NONE = 0,
    D3DMULTISAMPLE_2_SAMPLES = 1,
    D3DMULTISAMPLE_4_SAMPLES = 2,
    D3DMULTISAMPLE_FORCE_DWORD = 3      // Wrong but not needed
}

public enum GPUCLAMP : byte             // 3 bit value
{
    GPUCLAMP_WRAP = 0,
    GPUCLAMP_MIRROR = 1,
    GPUCLAMP_CLAMP_TO_LAST = 2,
    GPUCLAMP_MIRROR_ONCE_TO_LAST = 3,
    GPUCLAMP_CLAMP_HALFWAY = 4,
    GPUCLAMP_MIRROR_ONCE_HALFWAY = 5,
    GPUCLAMP_CLAMP_TO_BORDER = 6,
    GPUCLAMP_MIRROR_TO_BORDER = 7
}

public enum GPUSIGN : byte              // 2 bit value
{
    GPUSIGN_UNSIGNED = 0,
    GPUSIGN_SIGNED = 1,
    GPUSIGN_BIAS = 2,
    GPUSIGN_GAMMA = 3
}

public enum GPUCONSTANTTYPE : byte      // 2 bit value
{
    GPUCONSTANTTYPE_INVALID_TEXTURE = 0,
    GPUCONSTANTTYPE_INVALID_VERTEX = 1,
    GPUCONSTANTTYPE_TEXTURE = 2,
    GPUCONSTANTTYPE_VERTEX = 3
}

public enum GPUTEXTUREFORMAT : byte     // 6 bit value
{
    GPUTEXTUREFORMAT_1_REVERSE = 0,
    GPUTEXTUREFORMAT_1 = 1,
    GPUTEXTUREFORMAT_8 = 2,
    GPUTEXTUREFORMAT_1_5_5_5 = 3,
    GPUTEXTUREFORMAT_5_6_5 = 4,
    GPUTEXTUREFORMAT_6_5_5 = 5,
    GPUTEXTUREFORMAT_8_8_8_8 = 6,
    GPUTEXTUREFORMAT_2_10_10_10 = 7,
    GPUTEXTUREFORMAT_8_A = 8,
    GPUTEXTUREFORMAT_8_B = 9,
    GPUTEXTUREFORMAT_8_8 = 10,
    GPUTEXTUREFORMAT_Cr_Y1_Cb_Y0_REP = 11,
    GPUTEXTUREFORMAT_Y1_Cr_Y0_Cb_REP = 12,
    GPUTEXTUREFORMAT_16_16_EDRAM = 13,
    GPUTEXTUREFORMAT_8_8_8_8_A = 14,
    GPUTEXTUREFORMAT_4_4_4_4 = 15,
    GPUTEXTUREFORMAT_10_11_11 = 16,
    GPUTEXTUREFORMAT_11_11_10 = 17,
    GPUTEXTUREFORMAT_DXT1 = 18,
    GPUTEXTUREFORMAT_DXT2_3 = 19,
    GPUTEXTUREFORMAT_DXT4_5 = 20,
    GPUTEXTUREFORMAT_16_16_16_16_EDRAM = 21,
    GPUTEXTUREFORMAT_24_8 = 22,
    GPUTEXTUREFORMAT_24_8_FLOAT = 23,
    GPUTEXTUREFORMAT_16 = 24,
    GPUTEXTUREFORMAT_16_16 = 25,
    GPUTEXTUREFORMAT_16_16_16_16 = 26,
    GPUTEXTUREFORMAT_16_EXPAND = 27,
    GPUTEXTUREFORMAT_16_16_EXPAND = 28,
    GPUTEXTUREFORMAT_16_16_16_16_EXPAND = 29,
    GPUTEXTUREFORMAT_16_FLOAT = 30,
    GPUTEXTUREFORMAT_16_16_FLOAT = 31,
    GPUTEXTUREFORMAT_16_16_16_16_FLOAT = 32,
    GPUTEXTUREFORMAT_32 = 33,
    GPUTEXTUREFORMAT_32_32 = 34,
    GPUTEXTUREFORMAT_32_32_32_32 = 35,
    GPUTEXTUREFORMAT_32_FLOAT = 36,
    GPUTEXTUREFORMAT_32_32_FLOAT = 37,
    GPUTEXTUREFORMAT_32_32_32_32_FLOAT = 38,
    GPUTEXTUREFORMAT_32_AS_8 = 39,
    GPUTEXTUREFORMAT_32_AS_8_8 = 40,
    GPUTEXTUREFORMAT_16_MPEG = 41,
    GPUTEXTUREFORMAT_16_16_MPEG = 42,
    GPUTEXTUREFORMAT_8_INTERLACED = 43,
    GPUTEXTUREFORMAT_32_AS_8_INTERLACED = 44,
    GPUTEXTUREFORMAT_32_AS_8_8_INTERLACED = 45,
    GPUTEXTUREFORMAT_16_INTERLACED = 46,
    GPUTEXTUREFORMAT_16_MPEG_INTERLACED = 47,
    GPUTEXTUREFORMAT_16_16_MPEG_INTERLACED = 48,
    GPUTEXTUREFORMAT_DXN = 49,
    GPUTEXTUREFORMAT_8_8_8_8_AS_16_16_16_16 = 50,
    GPUTEXTUREFORMAT_DXT1_AS_16_16_16_16 = 51,
    GPUTEXTUREFORMAT_DXT2_3_AS_16_16_16_16 = 52,
    GPUTEXTUREFORMAT_DXT4_5_AS_16_16_16_16 = 53,
    GPUTEXTUREFORMAT_2_10_10_10_AS_16_16_16_16 = 54,
    GPUTEXTUREFORMAT_10_11_11_AS_16_16_16_16 = 55,
    GPUTEXTUREFORMAT_11_11_10_AS_16_16_16_16 = 56,
    GPUTEXTUREFORMAT_32_32_32_FLOAT = 57,
    GPUTEXTUREFORMAT_DXT3A = 58,
    GPUTEXTUREFORMAT_DXT5A = 59,
    GPUTEXTUREFORMAT_CTX1 = 60,
    GPUTEXTUREFORMAT_DXT3A_AS_1_1_1_1 = 61,
    GPUTEXTUREFORMAT_8_8_8_8_GAMMA_EDRAM = 62,
    GPUTEXTUREFORMAT_2_10_10_10_FLOAT_EDRAM = 63
}

public enum GPUCLAMPPOLICY : byte       // 1 bit value
{
    GPUCLAMPPOLICY_D3D = 0,
    GPUCLAMPPOLICY_OGL = 1
}

public enum GPUREQUESTSIZE : byte       // 2 bit value
{
    GPUREQUESTSIZE_256BIT = 0,
    GPUREQUESTSIZE_512BIT = 1
}

public enum GPUENDIAN : byte            // 2 bit value
{
    GPUENDIAN_NONE = 0,
    GPUENDIAN_8IN16 = 1,
    GPUENDIAN_8IN32 = 2,
    GPUENDIAN_16IN32 = 3
}

public enum GPUANISOFILTER : byte       // 3 bit value
{
    GPUANISOFILTER_DISABLED = 0,
    GPUANISOFILTER_MAX1TO1 = 1,
    GPUANISOFILTER_MAX2TO1 = 2,
    GPUANISOFILTER_MAX4TO1 = 3,
    GPUANISOFILTER_MAX8TO1 = 4,
    GPUANISOFILTER_MAX16TO1 = 5,
    GPUANISOFILTER_KEEP = 7
}

public enum GPUMIPFILTER : byte         // 2 bit value
{
    GPUMIPFILTER_POINT = 0,
    GPUMIPFILTER_LINEAR = 1,
    GPUMIPFILTER_BASEMAP = 2,
    GPUMIPFILTER_KEEP = 3
}

public enum GPUNUMFORMAT : byte         // 1 bit value
{
    GPUNUMFORMAT_FRACTION = 0,
    GPUNUMFORMAT_INTEGER = 1
}

public enum GPUSWIZZLE : byte           // 3 bit value
{
    GPUSWIZZLE_X = 0,
    GPUSWIZZLE_Y = 1,
    GPUSWIZZLE_Z = 2,
    GPUSWIZZLE_W = 3,
    GPUSWIZZLE_0 = 4,
    GPUSWIZZLE_1 = 5
}

public enum GPUMINMAGFILTER : byte      // 2 bit value
{
    GPUMINMAGFILTER_POINT = 0,
    GPUMINMAGFILTER_LINEAR = 1,
    GPUMINMAGFILTER_KEEP = 2
}

public enum GPUBORDERCOLOR : byte       // 2 bit value
{
    GPUBORDERCOLOR_ABGR_BLACK = 0,
    GPUBORDERCOLOR_ABGR_WHITE = 1,
    GPUBORDERCOLOR_ACBYCR_BLACK = 2,
    GPUBORDERCOLOR_ACBCRY_BLACK = 3
}

public enum GPUTRICLAMP : byte          // 2 bit value
{
    GPUTRICLAMP_NORMAL = 0,
    GPUTRICLAMP_ONE_SIXTH = 1,
    GPUTRICLAMP_ONE_FOURTH = 2,
    GPUTRICLAMP_THREE_EIGHTHS = 3
}

public enum GPUDIMENSION : byte         // 2 bit value
{
    GPUDIMENSION_1D = 0,
    GPUDIMENSION_2D = 1,
    GPUDIMENSION_3D = 2,
    GPUDIMENSION_CUBEMAP = 3
}
