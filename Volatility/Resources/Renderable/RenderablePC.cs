namespace Volatility.Resources.Renderable;

public class RenderablePC : RenderableBase
{
    public override Endian GetResourceEndian() => Endian.LE;
    public override Platform GetResourcePlatform() => Platform.TUB;

    public override void ParseFromStream(ResourceBinaryReader reader)
    {
        reader.SetEndianness(GetResourceEndian());

        reader.BaseStream.Seek(0x20, SeekOrigin.Begin);

        IndexBuffer = reader.ReadUInt32();
        VertexBuffer = reader.ReadUInt32();

        reader.BaseStream.Seek(0x0, SeekOrigin.Begin);

        base.ParseFromStream(reader);
    }

    public RenderablePC(string path) : base(path) { }
}
