namespace Volatility.Resources;

public class ShaderProgramBufferBase : Resource
{
    public override ResourceType ResourceType => ResourceType.RwShaderProgramBuffer;
    public override Endian ResourceEndian => Endian.Agnostic;
    public override Platform ResourcePlatform => Platform.Agnostic;

    public override void WriteToStream(ResourceBinaryWriter writer, Endian endianness)
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
