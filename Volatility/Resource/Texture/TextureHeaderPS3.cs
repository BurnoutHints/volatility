using System.Text;
using static Volatility.Utilities.DataUtilities;

namespace Volatility.Resource.Texture;

public class TextureHeaderPS3 : TextureHeaderBase
{
    public new static readonly Endian ResourceEndian = Endian.BE;

    public CELL_GCM_COLOR_FORMAT Format;
    public CELL_GCM_TEXTURE_DIMENSION CellDimension;
    public bool CubeMapEnable;
    public uint Remap;                 // TODO
    public CELL_GCM_LOCATION Location;
    public uint Pitch;
    public uint Offset;
    public nint Buffer;
    public StoreType StoreType;
    public uint StoreFlags;            // Seems to be unused

    public TextureHeaderPS3() : base() { }

    public TextureHeaderPS3(string path) : base(path) { }

    public override void PullInternalDimension()
    {
        Dimension = CubeMapEnable ? DIMENSION.DIMENSION_CUBE : CellDimension switch
        {
            CELL_GCM_TEXTURE_DIMENSION.CELL_GCM_TEXTURE_DIMENSION_1 => DIMENSION.DIMENSION_1D,
            CELL_GCM_TEXTURE_DIMENSION.CELL_GCM_TEXTURE_DIMENSION_2 => DIMENSION.DIMENSION_2D,
            CELL_GCM_TEXTURE_DIMENSION.CELL_GCM_TEXTURE_DIMENSION_3 => DIMENSION.DIMENSION_3D,
            _ => DIMENSION.DIMENSION_2D,
        };
    }

    public override void PullInternalFlags()
    {
        base.PullInternalFlags();
    }

    public override void PullInternalFormat() { }

    public override void PushInternalDimension()
    {
        CellDimension = Dimension switch
        {
            DIMENSION.DIMENSION_3D => CELL_GCM_TEXTURE_DIMENSION.CELL_GCM_TEXTURE_DIMENSION_3,
            DIMENSION.DIMENSION_CUBE => CELL_GCM_TEXTURE_DIMENSION.CELL_GCM_TEXTURE_DIMENSION_3,
            DIMENSION.DIMENSION_1D => CELL_GCM_TEXTURE_DIMENSION.CELL_GCM_TEXTURE_DIMENSION_1,
            _ => CELL_GCM_TEXTURE_DIMENSION.CELL_GCM_TEXTURE_DIMENSION_2,
        };

        StoreType = Dimension switch
        {
            DIMENSION.DIMENSION_1D => StoreType.TYPE_1D,
            DIMENSION.DIMENSION_2D => StoreType.TYPE_2D,
            DIMENSION.DIMENSION_3D => StoreType.TYPE_3D,
            DIMENSION.DIMENSION_CUBE => StoreType.TYPE_CUBE,
            _ => StoreType.TYPE_2D
        };

        CubeMapEnable = StoreType == StoreType.TYPE_CUBE;
    }

    public override void PushInternalFlags()
    {
        // Calculate pitch only if DXT and non-power of two texture
        switch (Format)
        {
            case CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_COMPRESSED_DXT1:
            case CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_COMPRESSED_DXT23:
            case CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_COMPRESSED_DXT45:
                CalculatePitchPS3(Width, Format == CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_COMPRESSED_DXT1 ? 8 : 16);
                break;
            default:
                break;
        }
    }

    public override void PushInternalFormat() { /* TODO But don't throw an error! */ }

    public override void WriteToStream(BinaryWriter writer)
    {
        writer.Write((byte)Format);
        writer.Write(MipmapLevels);
        writer.Write((byte)CellDimension);
        writer.Write(CubeMapEnable ? (byte)1 : (byte)0);
        writer.Write(Remap); // Does this need to be swapped?
        writer.Write(SwapEndian(Width));
        writer.Write(SwapEndian(Height));
        writer.Write(SwapEndian(Depth));
        writer.Write((byte)Location);
        writer.Write((byte)0); // Padding
        writer.Write(SwapEndian(Pitch));
        writer.Write(SwapEndian(Offset));
        writer.Write(SwapEndian((uint)Buffer));
        writer.Write(SwapEndian((int)StoreType));
        writer.Write(StoreFlags);

        // Padding that's usually just garbage data.
        writer.Write(Encoding.UTF8.GetBytes("Volatility"));
        writer.Write(new byte[0x2]);
    }

    public override void ParseFromStream(BinaryReader reader)
    {
        base.ParseFromStream(reader);

        Format = (CELL_GCM_COLOR_FORMAT)reader.ReadByte();
        MipmapLevels = reader.ReadByte();
        CellDimension = (CELL_GCM_TEXTURE_DIMENSION)reader.ReadByte();
        CubeMapEnable = reader.ReadByte() != 0 ? true : false;
        Remap = reader.ReadUInt32(); // Does this need to be swapped?
        Width = SwapEndian(reader.ReadUInt16());
        Height = SwapEndian(reader.ReadUInt16());
        Depth = SwapEndian(reader.ReadUInt16());
        Location = (CELL_GCM_LOCATION)reader.ReadByte();
        reader.BaseStream.Seek(1, SeekOrigin.Current);
        Pitch = SwapEndian(reader.ReadUInt32());
        Offset = SwapEndian(reader.ReadUInt32());
        Buffer = (nint)SwapEndian(reader.ReadUInt32());
        StoreType = (StoreType)SwapEndian(reader.ReadInt32());
        StoreFlags = reader.ReadUInt32();   // They're flags, I doubt they need to be swapped
    }
}

public enum CELL_GCM_COLOR_FORMAT : byte
{
    CELL_GCM_TEXTURE_B8 = 129,
    CELL_GCM_TEXTURE_A1R5G5B5 = 130,
    CELL_GCM_TEXTURE_A4R4G4B4 = 131,
    CELL_GCM_TEXTURE_R5G6B5 = 132,
    CELL_GCM_TEXTURE_A8R8G8B8 = 133,
    CELL_GCM_TEXTURE_COMPRESSED_DXT1 = 134,
    CELL_GCM_TEXTURE_COMPRESSED_DXT23 = 135,
    CELL_GCM_TEXTURE_COMPRESSED_DXT45 = 136,
    CELL_GCM_TEXTURE_G8B8 = 139,
    CELL_GCM_TEXTURE_R6G5B5 = 143,
    CELL_GCM_TEXTURE_DEPTH24_D8 = 144,
    CELL_GCM_TEXTURE_DEPTH24_D8_FLOAT = 145,
    CELL_GCM_TEXTURE_DEPTH16 = 146,
    CELL_GCM_TEXTURE_DEPTH16_FLOAT = 147,
    CELL_GCM_TEXTURE_X16 = 148,
    CELL_GCM_TEXTURE_Y16_X16 = 149,
    CELL_GCM_TEXTURE_R5G5B5A1 = 151,
    CELL_GCM_TEXTURE_COMPRESSED_HILO8 = 152,
    CELL_GCM_TEXTURE_COMPRESSED_HILO_S8 = 153,
    CELL_GCM_TEXTURE_W16_Z16_Y16_X16_FLOAT = 154,
    CELL_GCM_TEXTURE_W32_Z32_Y32_X32_FLOAT = 155,
    CELL_GCM_TEXTURE_X32_FLOAT = 156,
    CELL_GCM_TEXTURE_D1R5G5B5 = 157,
    CELL_GCM_TEXTURE_D8R8G8B8 = 158,
    CELL_GCM_TEXTURE_Y16_X16_FLOAT = 159,
    CELL_GCM_TEXTURE_COMPRESSED_B8R8_G8R8 = 173,
    CELL_GCM_TEXTURE_COMPRESSED_R8B8_R8G8 = 174
}

public enum CELL_GCM_TEXTURE_DIMENSION : byte
{
    CELL_GCM_TEXTURE_DIMENSION_1 = 1,
    CELL_GCM_TEXTURE_DIMENSION_2 = 2,
    CELL_GCM_TEXTURE_DIMENSION_3 = 3
}

public enum CELL_GCM_LOCATION : byte
{
    CELL_GCM_LOCATION_LOCAL = 1,    // Local memory
    CELL_GCM_LOCATION_MAIN = 2      // Main memory 
}

public enum StoreType : int
{
    TYPE_NA = -1,
    TYPE_1D = 1,
    TYPE_2D = 2,
    TYPE_3D = 3,
    TYPE_CUBE = 0x10002,
    TYPE_FORCEENUMSIZEINT = 0x7FFFFFFF
}