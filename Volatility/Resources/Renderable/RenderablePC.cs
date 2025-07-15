using Volatility.Extensions;

namespace Volatility.Resources;

public class RenderablePC : RenderableBase
{
    public override Endian GetResourceEndian() => Endian.LE;
    public override Platform GetResourcePlatform() => Platform.TUB;

    public override void ParseFromStream(BinaryReader reader, Endian n = Endian.Agnostic)
    {
        reader.BaseStream.Seek(0x20, SeekOrigin.Begin);

        IndexBuffer = reader.ReadPointer(GetResourceArch(), n);
        VertexBuffer = reader.ReadPointer(GetResourceArch(), n);

        reader.BaseStream.Seek(0x0, SeekOrigin.Begin);

        base.ParseFromStream(reader, n);
    }

    public RenderablePC(string path, Endian endianness = Endian.Agnostic) : base(path, endianness) { }
}
