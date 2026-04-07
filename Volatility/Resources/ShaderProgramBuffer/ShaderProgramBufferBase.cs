namespace Volatility.Resources;

public class ShaderProgramBufferBase : Resource
{
    public override ResourceType GetResourceType() => ResourceType.RwShaderProgramBuffer;
    public override Endian GetResourceEndian() => Endian.Agnostic;
    public override Platform GetResourcePlatform() => Platform.Agnostic;

    public override void WriteToStream(EndianAwareBinaryWriter writer, Endian endianness)
    {
        base.WriteToStream(writer, endianness);
    }
    public override void ParseFromStream(ResourceBinaryReader reader, Endian endianness)
    {
        base.ParseFromStream(reader, endianness);
    }

    public ShaderProgramBufferBase() : base() { }

    public ShaderProgramBufferBase(string path) : base(path) { }
}
