using System.Text;
using Volatility.Utilities;

namespace Volatility.TextureHeader
{
    public class TextureHeaderPC : TextureHeaderBase
    {
        private D3DFORMAT _Format = D3DFORMAT.D3DFMT_UNKNOWN;
        public D3DFORMAT Format
        {
            get => _Format;
            set 
            {
                _Format = value;
                PushInternalFormat(); 
            }
        }

        public readonly IntPtr TextureDataPtr;      // Set at game runtime, 0
        public readonly IntPtr TextureInterfacePtr; // Set at game runtime, 0
        public uint Unknown0;                       // Flags
        public readonly ushort MemoryClass = 1;     // D3DPool, Always 1
        public byte Unknown1;                       // Flags
        public byte Unknown2;                       // Flags
        private byte[] OutputFormat = new byte[4];  // Needs to be 4 bytes long
        public ushort Width;                        // Width in px
        public ushort Height;                       // Height in px
        public readonly byte Depth = 1;             // Always 1 for Burnout textures
        public byte MipLevels;                      // Amount of mipmaps
        public TEXTURETYPE TextureType;             // Dimension in BPR
        public byte Flags;                          // Flags

        public override void WriteToStream(BinaryWriter writer)
        {
            PushInternalFlags();
            PushInternalFormat();

            writer.Write(TextureDataPtr.ToInt32());
            writer.Write(TextureInterfacePtr.ToInt32());
            writer.Write(Unknown0);     // Unknown
            writer.Write(MemoryClass);
            writer.Write(Unknown1);     // Unknown
            writer.Write(Unknown2);     // Unknown
            writer.Write(OutputFormat);
            writer.Write(Width);
            writer.Write(Height);
            writer.Write(Depth);
            writer.Write(MipLevels);
            writer.Write((byte)TextureType);
            writer.Write(Flags);
            writer.Write(new byte[4]);  // Padding
        }

        public override void PushInternalFormat()
        {
            byte[] outputFormat = new byte[4];
            switch (Format)
            {
                case D3DFORMAT.D3DFMT_DXT1:
                    outputFormat = Encoding.UTF8.GetBytes("DXT1");
                    break;
                case D3DFORMAT.D3DFMT_DXT3:
                    outputFormat = Encoding.UTF8.GetBytes("DXT3");
                    break;
                case D3DFORMAT.D3DFMT_DXT5:
                    outputFormat = Encoding.UTF8.GetBytes("DXT5");
                    break;
                case D3DFORMAT.D3DFMT_G8R8_G8B8:
                    outputFormat = Encoding.UTF8.GetBytes("GRGB");
                    break;
                case D3DFORMAT.D3DFMT_MULTI2_ARGB8:
                    outputFormat = Encoding.UTF8.GetBytes("MET1");
                    break;
                default:
                    outputFormat[0] = DataUtilities.TrimIntToByte((int)Format); // Use literal value
                    break;

            }
            OutputFormat = outputFormat;
        }

        public override void PullInternalFormat()
        {
            string tryParseString = Encoding.ASCII.GetString(OutputFormat);

            D3DFORMAT outputFormat;
            switch (tryParseString)
            {
                case "DXT1":
                    outputFormat = D3DFORMAT.D3DFMT_DXT1;
                    break;
                case "DXT3":
                    outputFormat = D3DFORMAT.D3DFMT_DXT3;
                    break;
                case "DXT5":
                    outputFormat = D3DFORMAT.D3DFMT_DXT5;
                    break;
                case "GRGB":
                    outputFormat = D3DFORMAT.D3DFMT_G8R8_G8B8;
                    break;
                case "MET1":
                    outputFormat = D3DFORMAT.D3DFMT_MULTI2_ARGB8;
                    break;
                default:
                    outputFormat = (D3DFORMAT)BitConverter.ToInt32(OutputFormat);
                    break;
            }
            Format = outputFormat;
        }

        public override void PushInternalFlags()
        {
            Unknown1 = DataUtilities.TrimIntToByte(WorldTexture || GRTexture ? 1 : 0);
            Unknown2 = DataUtilities.TrimIntToByte(WorldTexture || PropTexture || GRTexture ? 1 : 0);
            Flags = DataUtilities.TrimIntToByte(WorldTexture || PropTexture || GRTexture ? 8 : 0);
        }
        public override void PullInternalFlags()
        {
            throw new NotImplementedException();
        }

        public override void PushInternalDimension()
        {
            TEXTURETYPE outputDimension;
            switch (Dimension)
            {
                case DIMENSION.DIMENSION_1D:
                case DIMENSION.DIMENSION_CUBE:
                    outputDimension = TEXTURETYPE.TEXTURETYPE_1D;
                    break;
                case DIMENSION.DIMENSION_3D:
                    outputDimension = TEXTURETYPE.TEXTURETYPE_3D;
                    break;
                case DIMENSION.DIMENSION_2D:
                default:
                    outputDimension = TEXTURETYPE.TEXTURETYPE_2D;
                    break;
            }
            TextureType = outputDimension;
        }

        public override void PullInternalDimension()
        {
            DIMENSION outputDimension;
            switch (TextureType)    // Idk how to handle 1D textures, doc says 1D is cube in TUB
            {
                case TEXTURETYPE.TEXTURETYPE_1D:
                    outputDimension = DIMENSION.DIMENSION_CUBE;
                    break;
                case TEXTURETYPE.TEXTURETYPE_3D:
                    outputDimension = DIMENSION.DIMENSION_3D;
                    break;
                case TEXTURETYPE.TEXTURETYPE_2D:
                case TEXTURETYPE.TEXTURETYPE_UNKNOWN2D:
                default:
                    outputDimension = DIMENSION.DIMENSION_2D;
                    break;
            }
            Dimension = outputDimension;
        }
    }

    public enum D3DFORMAT : int
    {
        D3DFMT_UNKNOWN = 0,
        D3DFMT_R8G8B8 = 20,
        D3DFMT_A8R8G8B8 = 21,
        D3DFMT_X8R8G8B8 = 22,
        D3DFMT_R5G6B5 = 23,
        D3DFMT_X1R5G5B5 = 24,
        D3DFMT_A1R5G5B5 = 25,
        D3DFMT_A4R4G4B4 = 26,
        D3DFMT_R3G3B2 = 27,
        D3DFMT_A8 = 28,
        D3DFMT_A8R3G3B2 = 29,
        D3DFMT_X4R4G4B4 = 30,
        D3DFMT_A2B10G10R10 = 31,
        D3DFMT_A8B8G8R8 = 32,
        D3DFMT_X8B8G8R8 = 33,
        D3DFMT_G16R16 = 34,
        D3DFMT_A2R10G10B10 = 35,
        D3DFMT_A16B16G16R16 = 36,
        D3DFMT_A8P8 = 40,
        D3DFMT_P8 = 41,
        D3DFMT_L8 = 50,
        D3DFMT_A8L8 = 51,
        D3DFMT_A4L4 = 52,
        D3DFMT_V8U8 = 60,
        D3DFMT_L6V5U5 = 61,
        D3DFMT_X8L8V8U8 = 62,
        D3DFMT_Q8W8V8U8 = 63,
        D3DFMT_V16U16 = 64,
        D3DFMT_A2W10V10U10 = 67,
        D3DFMT_D16_LOCKABLE = 70,
        D3DFMT_D32 = 71,
        D3DFMT_D15S1 = 73,
        D3DFMT_D24S8 = 75,
        D3DFMT_D24X8 = 77,
        D3DFMT_D24X4S4 = 79,
        D3DFMT_D16 = 80,
        D3DFMT_L16 = 81,
        D3DFMT_D32F_LOCKABLE = 82,
        D3DFMT_D24FS8 = 83,
        D3DFMT_D32_LOCKABLE = 84,
        D3DFMT_S8_LOCKABLE = 85,
        D3DFMT_VERTEXDATA = 100,
        D3DFMT_INDEX16 = 101,
        D3DFMT_INDEX32 = 102,
        D3DFMT_Q16W16V16U16 = 110,
        D3DFMT_R16F = 111,
        D3DFMT_G16R16F = 112,
        D3DFMT_A16B16G16R16F = 113,
        D3DFMT_R32F = 114,
        D3DFMT_G32R32F = 115,
        D3DFMT_A32B32G32R32F = 116,
        D3DFMT_CxV8U8 = 117,
        D3DFMT_A1 = 118,
        D3DFMT_A2B10G10R10_XR_BIAS = 119,
        D3DFMT_BINARYBUFFER = 199,
        D3DFMT_DXT1 = -1,
        D3DFMT_DXT3 = -3,
        D3DFMT_DXT5 = -5,
        D3DFMT_G8R8_G8B8 = -8,
        D3DFMT_MULTI2_ARGB8 = -9,
        D3DFMT_R8G8_B8G8 = -10,
        D3DFMT_UYVY = -11,
        D3DFMT_YUY2 = -12
    }

    public enum TEXTURETYPE : byte
    {
        TEXTURETYPE_2D = 0,
        TEXTURETYPE_1D = 1,
        TEXTURETYPE_3D = 2,
        TEXTURETYPE_UNKNOWN2D = 3
    }
}
