namespace Volatility.Resources;

public class RenderableBPR : RenderableBase
{
    public override Endian GetResourceEndian() => Endian.LE;
    public override Platform GetResourcePlatform() => Platform.BPR;

    public override void ParseFromStream(ResourceBinaryReader reader, Endian endianness = Endian.Agnostic)
    {
        reader.SetEndianness(GetResourceEndian());

        reader.BaseStream.Seek(0x20, SeekOrigin.Begin);

        IndexBuffer = reader.ReadUInt32();
        VertexBuffer = reader.ReadUInt32();

        reader.BaseStream.Seek(0x0, SeekOrigin.Begin);

        base.ParseFromStream(reader, endianness);
    }

    public RenderableBPR(string path, Endian endianness = Endian.Agnostic) : base(path, endianness) { }
}

public enum D3D11_PRIMITIVE_TOPOLOGY
{
    UNDEFINED = 0,
    POINTLIST = 1,
    LINELIST = 2,
    LINESTRIP = 3,
    TRIANGLELIST = 4,
    TRIANGLESTRIP = 5,
    LINELIST_ADJ = 10,
    LINESTRIP_ADJ = 11,
    TRIANGLELIST_ADJ = 12,
    TRIANGLESTRIP_ADJ = 13,
    CONTROL_POINT_PATCHLIST_1 = 33,
    CONTROL_POINT_PATCHLIST_2 = 34,
    CONTROL_POINT_PATCHLIST_3 = 35,
    CONTROL_POINT_PATCHLIST_4 = 36,
    CONTROL_POINT_PATCHLIST_5 = 37,
    CONTROL_POINT_PATCHLIST_6 = 38,
    CONTROL_POINT_PATCHLIST_7 = 39,
    CONTROL_POINT_PATCHLIST_8 = 40,
    CONTROL_POINT_PATCHLIST_9 = 41,
    CONTROL_POINT_PATCHLIST_10 = 42,
    CONTROL_POINT_PATCHLIST_11 = 43,
    CONTROL_POINT_PATCHLIST_12 = 44,
    CONTROL_POINT_PATCHLIST_13 = 45,
    CONTROL_POINT_PATCHLIST_14 = 46,
    CONTROL_POINT_PATCHLIST_15 = 47,
    CONTROL_POINT_PATCHLIST_16 = 48,
    CONTROL_POINT_PATCHLIST_17 = 49,
    CONTROL_POINT_PATCHLIST_18 = 50,
    CONTROL_POINT_PATCHLIST_19 = 51,
    CONTROL_POINT_PATCHLIST_20 = 52,
    CONTROL_POINT_PATCHLIST_21 = 53,
    CONTROL_POINT_PATCHLIST_22 = 54,
    CONTROL_POINT_PATCHLIST_23 = 55,
    CONTROL_POINT_PATCHLIST_24 = 56,
    CONTROL_POINT_PATCHLIST_25 = 57,
    CONTROL_POINT_PATCHLIST_26 = 58,
    CONTROL_POINT_PATCHLIST_27 = 59,
    CONTROL_POINT_PATCHLIST_28 = 60,
    CONTROL_POINT_PATCHLIST_29 = 61,
    CONTROL_POINT_PATCHLIST_30 = 62,
    CONTROL_POINT_PATCHLIST_31 = 63,
    CONTROL_POINT_PATCHLIST_32 = 64
}
