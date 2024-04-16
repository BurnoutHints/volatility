using System.Collections;

namespace Volatility.TextureHeader
{
    internal class TextureHeaderX360 : TextureHeaderBase
    {
        public BitArray GPUTEXTURE_FETCH_CONSTANT = new BitArray(288);

        public bool Tiled;      // True when importing retail content
        public BitArray Pitch = new BitArray(new bool[9] { false, false, false, false, true, false, false, false, false });
        public BitArray Padding = new BitArray(1);

        public GPUDIMENSION GPUDimension = GPUDIMENSION.GPUDIMENSION_2D;

        public TextureHeaderX360() : base() {}

        public TextureHeaderX360(string path) : base(path) { }

        public override void PullInternalDimension()
        {
            DIMENSION OutputDimension = GPUDimension switch
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

        public override void PullInternalFlags() => throw new NotImplementedException();

        public override void PullInternalFormat() => throw new NotImplementedException();

        public override void PushInternalDimension()
        {
            var OutputDimension = Dimension switch
            {
                DIMENSION.DIMENSION_1D => GPUDIMENSION.GPUDIMENSION_1D,
                DIMENSION.DIMENSION_3D => GPUDIMENSION.GPUDIMENSION_3D,
                DIMENSION.DIMENSION_CUBE => GPUDIMENSION.GPUDIMENSION_CUBEMAP,
                _ => GPUDIMENSION.GPUDIMENSION_2D,
            };
            GPUDimension = OutputDimension;
        }

        // parse GPUTEXTURE_FETCH_CONSTANT
        public override void PushInternalFlags() => throw new NotImplementedException();

        public override void PushInternalFormat() => throw new NotImplementedException();

        public override void WriteToStream(BinaryWriter writer) => throw new NotImplementedException();

        public override void ParseFromStream(BinaryReader reader) => throw new NotImplementedException();
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

    public enum GPUDIMENSION : byte         // 2 bit value
    {
        GPUDIMENSION_1D = 0,
        GPUDIMENSION_2D = 1,
        GPUDIMENSION_3D = 2,
        GPUDIMENSION_CUBEMAP = 3
    }
}
