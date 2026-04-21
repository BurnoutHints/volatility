namespace Volatility.Resources;

[ResourceDefinition(ResourceType.RwShaderProgramBuffer)]
public class ShaderProgramBufferBase : Resource
{
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
