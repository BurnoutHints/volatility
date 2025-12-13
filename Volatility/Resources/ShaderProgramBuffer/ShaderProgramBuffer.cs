using static Volatility.Utilities.DataUtilities;

namespace Volatility.Resources.ShaderProgramBuffer;

// WIP - based on Remastered's ShaderProgramBuffer - Will expand to other platforms soon

public class ShaderProgramBuffer : Resource
{
    public override ResourceType GetResourceType() => ResourceType.RwShaderProgramBuffer;
    public override Endian GetResourceEndian() => Endian.LE;
    public override Platform GetResourcePlatform() => Platform.Agnostic;

    public ShaderType CompiledShaderType;

    public List<GlobalReference> GlobalsReferences;

    public List<Sampler> Samplers;

    public uint CompiledShaderSize;

    public override void WriteToStream(EndianAwareBinaryWriter writer, Endian endianness = Endian.Agnostic)
    {
        base.WriteToStream(writer, endianness);

        writer.Write((uint)CompiledShaderType); // Type
        writer.Write((uint)0); // Unknown
        writer.Write(x64Switch(GetResourceArch() == Arch.x64, 0)); // Runtime ptr
        writer.Write(GlobalsReferences.Count > 0 ? 1 : 0);
        writer.Write((uint)0); // Unknown
        writer.Write(CompiledShaderSize);
        // writer.Write(); // TODO: write all samplers and globals, then get size
        // writer.Write(); // TODO: Sampler count
        // writer.Write(); // TODO: Unknown block
        // writer.Write(); // TODO: Sampler array
    }
    public override void ParseFromStream(ResourceBinaryReader reader, Endian endianness = Endian.Agnostic)
    {
        base.ParseFromStream(reader, endianness);
        
        CompiledShaderType = (ShaderType)reader.ReadUInt32();
        
        reader.BaseStream.Seek(0x14, SeekOrigin.Begin);
        CompiledShaderSize = reader.ReadUInt32();
    }

    public ShaderProgramBuffer() : base() { }

    public ShaderProgramBuffer(string path) : base(path) { }

    public enum ShaderType : uint
    {
        VertexShader,
        PixelShader
    };

    public struct GlobalReference
    {
        public string ConstantName;
    }

    public struct Sampler
    {
        public string Name;
        public byte Index;
        public byte Unknown0;
        public ushort Unknown1;
    }
}
