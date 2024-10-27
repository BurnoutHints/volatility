using System.Text;
using static Volatility.Utilities.DataUtilities;

namespace Volatility.Resource.Texture;

public class TextureHeaderPC : TextureHeaderBase
{
    protected override Endian GetResourceEndian() => Endian.LE;

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

    public readonly nint TextureDataPtr;      // Set at game runtime, 0
    public readonly nint TextureInterfacePtr; // Set at game runtime, 0
    public uint Unknown0;                       // Flags
    public readonly ushort MemoryClass = 1;     // D3DPool, Always 1
    public byte Unknown1;                       // Flags
    public byte Unknown2;                       // Flags
    private byte[] OutputFormat = new byte[4];  // Needs to be 4 bytes long
    public TEXTURETYPE TextureType;             // Dimension in BPR
    public byte Flags;                          // Flags

    public TextureHeaderPC() : base() { }

    public TextureHeaderPC(string path) : base(path) { }

    public override void WriteToStream(BinaryWriter writer)
    {
        PushAll(); // Need to determine if should be moved

        writer.Write(TextureDataPtr.ToInt32());
        writer.Write(TextureInterfacePtr.ToInt32());
        writer.Write(Unknown0);     // Unknown
        writer.Write(MemoryClass);
        writer.Write(Unknown1);     // Unknown
        writer.Write(Unknown2);     // Unknown
        writer.Write(OutputFormat);

        // Not validated!!
        // TODO: Validate correct variable size
        writer.Write(Width);
        writer.Write(Height);
        writer.Write((byte)Depth);
        writer.Write(MipmapLevels);

        writer.Write((byte)TextureType);
        writer.Write(Flags);
        writer.Write(new byte[4]);  // Padding
    }

    public override void ParseFromStream(BinaryReader reader)
    {
        base.ParseFromStream(reader);

        reader.BaseStream.Seek(8, SeekOrigin.Begin);    // Skip over Data & Interface pointers
        Unknown0 = reader.ReadUInt32();
        reader.BaseStream.Seek(2, SeekOrigin.Current);  // Skip over MemoryClass
        Unknown1 = reader.ReadByte();
        Unknown2 = reader.ReadByte();
        OutputFormat = reader.ReadBytes(4);
        Width = reader.ReadUInt16();
        Height = reader.ReadUInt16();
        reader.BaseStream.Seek(1, SeekOrigin.Current);
        MipmapLevels = reader.ReadByte();
        TextureType = (TEXTURETYPE)reader.ReadByte();
        Flags = reader.ReadByte();
        // Skip reading 4 byte padding
    }

    public override void PushInternalFormat()
    {
        byte[] outputFormat = Format switch
        {
            D3DFORMAT.D3DFMT_DXT1 => Encoding.UTF8.GetBytes("DXT1"),
            D3DFORMAT.D3DFMT_DXT3 => Encoding.UTF8.GetBytes("DXT3"),
            D3DFORMAT.D3DFMT_DXT5 => Encoding.UTF8.GetBytes("DXT5"),
            D3DFORMAT.D3DFMT_G8R8_G8B8 => Encoding.UTF8.GetBytes("GRGB"),
            D3DFORMAT.D3DFMT_MULTI2_ARGB8 => Encoding.UTF8.GetBytes("MET1"),
            _ => BitConverter.GetBytes((int)Format), // Use literal value
        };
        OutputFormat = outputFormat;
    }

    public override void PullInternalFormat()
    {
        string tryParseString = Encoding.ASCII.GetString(OutputFormat);
        var outputFormat = tryParseString switch
        {
            "DXT1" => D3DFORMAT.D3DFMT_DXT1,
            "DXT3" => D3DFORMAT.D3DFMT_DXT3,
            "DXT5" => D3DFORMAT.D3DFMT_DXT5,
            "GRGB" => D3DFORMAT.D3DFMT_G8R8_G8B8,
            "MET1" => D3DFORMAT.D3DFMT_MULTI2_ARGB8,
            _ => (D3DFORMAT)BitConverter.ToInt32(OutputFormat),
        };
        Format = outputFormat;
    }

    public override void PushInternalFlags()
    {
        Unknown1 = TrimIntToByte(WorldTexture || GRTexture ? 1 : 0);
        Unknown2 = TrimIntToByte(WorldTexture || PropTexture || GRTexture ? 1 : 0);
        Flags = TrimIntToByte(WorldTexture || PropTexture || GRTexture ? 8 : 0);
    }
    public override void PullInternalFlags()
    {
        // TODO: More accurate/efficient flag calcuation!

        // Assuming Unknown 2 is probably world, and Unknown 1 is GR?
        WorldTexture = Unknown2 != 0;
        GRTexture = Unknown1 != 0;

        // We're just a prop in this cruel game of life
        PropTexture = Unknown1 == 0 && Unknown2 != 0 && Flags != 0;

        // Run after, if directory exists then we use that instead
        base.PullInternalFlags();
    }

    public override void PushInternalDimension()
    {
        TextureType = Dimension switch
        {
            DIMENSION.DIMENSION_1D or DIMENSION.DIMENSION_CUBE => TEXTURETYPE.TEXTURETYPE_1D,
            DIMENSION.DIMENSION_3D => TEXTURETYPE.TEXTURETYPE_3D,
            _ => TEXTURETYPE.TEXTURETYPE_2D,
        };
    }

    public override void PullInternalDimension()
    {
        Dimension = TextureType switch    // Idk how to handle 1D textures, doc says 1D is cube in TUB
        {
            TEXTURETYPE.TEXTURETYPE_1D => DIMENSION.DIMENSION_CUBE,
            TEXTURETYPE.TEXTURETYPE_3D => DIMENSION.DIMENSION_3D,
            _ => DIMENSION.DIMENSION_2D,
        };
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
    // Formats represented by strings
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
