using System.Collections;
using Volatility.Utilities;

namespace Volatility.TextureHeader;

public class TextureHeaderX360 : TextureHeaderBase
{
    public GPUTEXTURE_FETCH_CONSTANT GPUTEXTURE_FETCH_CONSTANT;    

    public TextureHeaderX360() : base() {}
    
    public TextureHeaderX360(string path) : base(path) { }
    
    public override void PullInternalDimension()
    {
        DIMENSION OutputDimension = GPUTEXTURE_FETCH_CONSTANT.Dimension switch
        {
            GPUDIMENSION.GPUDIMENSION_1D => DIMENSION.DIMENSION_1D,
            GPUDIMENSION.GPUDIMENSION_3D => DIMENSION.DIMENSION_3D,
            GPUDIMENSION.GPUDIMENSION_CUBEMAP => DIMENSION.DIMENSION_CUBE,
            _ => DIMENSION.DIMENSION_2D,
        };
        // Directly set the internal field to not trigger a push
        // Is this good a good practice?
        _Dimension = OutputDimension;
    }
    
    public override void PullInternalFlags()
    {
        // TODO: Implement better parser
        // Not accurate. Only prevents 3D & cubemaps from being GR/Prop Textures
        GRTexture = PropTexture = GPUTEXTURE_FETCH_CONSTANT.Dimension switch
        {
            GPUDIMENSION.GPUDIMENSION_1D => true,
            GPUDIMENSION.GPUDIMENSION_3D => false,
            GPUDIMENSION.GPUDIMENSION_CUBEMAP => false,
            _ => true,
        };
        WorldTexture = true;
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
        GPUTEXTURE_FETCH_CONSTANT.Dimension = OutputDimension;
    }
    
    // parse GPUTEXTURE_FETCH_CONSTANT
    public override void PushInternalFlags()
    {
        Console.WriteLine("TODO: Parse GPUTEXTURE_FETCH_CONSTANT");
    }

    public override void PushInternalFormat() 
    {
        // Not needed for 360
    }

    public override void WriteToStream(BinaryWriter writer) => throw new NotImplementedException();

    public override void ParseFromStream(BinaryReader reader)
    {
        // TODO: Parse everything before
        reader.BaseStream.Seek(28, SeekOrigin.Begin);
        GPUTEXTURE_FETCH_CONSTANT = GPUTEXTURE_FETCH_CONSTANT.FromPacked(reader.ReadBytes(24));
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
    public dynamic Size;                      // 32 bits, GPUTEXTURESIZE union
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

    public static GPUTEXTURE_FETCH_CONSTANT FromPacked(byte[] bytes)
    {
        BitReader bitReader = new BitReader(bytes);
        return new GPUTEXTURE_FETCH_CONSTANT
        {
            Tiled = bitReader.ReadBits(1) != 0,
            Pitch = (ushort)bitReader.ReadBits(10), // 9 bits + 1 bit padding
            MultiSample = (GPUMULTISAMPLE_TYPE)bitReader.ReadBits(2),
            ClampZ = (GPUCLAMP)bitReader.ReadBits(3),
            ClampY = (GPUCLAMP)bitReader.ReadBits(3),
            ClampX = (GPUCLAMP)bitReader.ReadBits(3),
            SignW = (GPUSIGN)bitReader.ReadBits(2),
            SignZ = (GPUSIGN)bitReader.ReadBits(2),
            SignY = (GPUSIGN)bitReader.ReadBits(2),
            SignX = (GPUSIGN)bitReader.ReadBits(2),
            Type = (GPUCONSTANTTYPE)bitReader.ReadBits(2),
            BaseAddress = bitReader.ReadBits(20),
            ClampPolicy = (GPUCLAMPPOLICY)bitReader.ReadBits(1),
            Stacked = bitReader.ReadBits(1) != 0,
            RequestSize = (GPUREQUESTSIZE)bitReader.ReadBits(2),
            Endian = (GPUENDIAN)bitReader.ReadBits(2),
            DataFormat = (GPUTEXTUREFORMAT)bitReader.ReadBits(6),
            Size = bitReader.ReadBits(32), // Assumes dynamic is handled as uint here
            BorderSize = (byte)bitReader.ReadBits(4), // 1 bit + 3 bits padding
            AnisoFilter = (GPUANISOFILTER)bitReader.ReadBits(3),
            MipFilter = (GPUMIPFILTER)bitReader.ReadBits(2),
            MinFilter = (GPUMINMAGFILTER)bitReader.ReadBits(2),
            MagFilter = (GPUMINMAGFILTER)bitReader.ReadBits(2),
            ExpAdjust = (byte)bitReader.ReadBits(6),
            SwizzleW = (GPUSWIZZLE)bitReader.ReadBits(3),
            SwizzleZ = (GPUSWIZZLE)bitReader.ReadBits(3),
            SwizzleY = (GPUSWIZZLE)bitReader.ReadBits(3),
            SwizzleX = (GPUSWIZZLE)bitReader.ReadBits(3),
            NumFormat = (GPUNUMFORMAT)bitReader.ReadBits(1),
            GradExpAdjustV = (byte)bitReader.ReadBits(5),
            GradExpAdjustH = (byte)bitReader.ReadBits(5),
            LODBias = (ushort)bitReader.ReadBits(10),
            MinAnisoWalk = bitReader.ReadBits(1) != 0,
            MagAnisoWalk = bitReader.ReadBits(1) != 0,
            MaxMipLevel = (byte)bitReader.ReadBits(4),
            MinMipLevel = (byte)bitReader.ReadBits(4),
            VolMinFilter = (GPUMINMAGFILTER)bitReader.ReadBits(1),
            VolMagFilter = (GPUMINMAGFILTER)bitReader.ReadBits(1),
            MipAddress = bitReader.ReadBits(20),
            PackedMips = bitReader.ReadBits(1) != 0,
            Dimension = (GPUDIMENSION)bitReader.ReadBits(2),
            AnisoBias = (byte)bitReader.ReadBits(4),
            TriClamp = (GPUTRICLAMP)bitReader.ReadBits(2),
            ForceBCWToMax = bitReader.ReadBits(1) != 0,
            BorderColor = (GPUBORDERCOLOR)bitReader.ReadBits(2)
        };
    }
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
