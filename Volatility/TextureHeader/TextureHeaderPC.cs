using System.Text;
using Volatility.Utilities;

namespace Volatility.TextureHeader
{
    public class TextureHeaderPC : TextureHeaderBase
    {
        private D3DFORMAT _Format = D3DFORMAT.D3DFMT_UNKNOWN;
        public D3DFORMAT Format
        {
            get { return _Format; }
            set 
            {
                _Format = value;
                PushInternalFormat(); 
            }
        }

        private DIMENSION _Dimension = DIMENSION.DIMENSION_2D;
        public DIMENSION Dimension
        {
            get { return _Dimension; }
            set 
            {
                _Dimension = value;
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
        public byte TextureType;                    // Dimension in BPR
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
            writer.Write(TextureType);
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

            byte outputDimension;
            switch (Dimension)
            {
                case DIMENSION.DIMENSION_1D:
                case DIMENSION.DIMENSION_CUBE:
                    outputDimension = 1;
                    break;
                case DIMENSION.DIMENSION_3D:
                    outputDimension = 2;
                    break;
                case DIMENSION.DIMENSION_2D:
                default:
                    outputDimension = 0;
                    break;
            }
            TextureType = outputDimension;
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

            DIMENSION outputDimension;
            switch (TextureType)    // Idk how to handle 1D textures, doc says 1D is cube in TUB
            {
                case 1:
                    outputDimension = DIMENSION.DIMENSION_CUBE;
                    break;
                case 2:
                    outputDimension = DIMENSION.DIMENSION_3D;
                    break;
                case 0:
                case 3:
                default:
                    outputDimension = DIMENSION.DIMENSION_2D;
                    break;
            }
            Dimension = outputDimension;
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
    }
}
