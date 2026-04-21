namespace Volatility.Resources;

// The BinaryFile resource type is a base type used in several
// other resource types, including Splicer, World Painter 2D,
// and Generic RWAC Wave Content.

// Learn More:
// https://burnout.wiki/wiki/Binary_File

[ResourceDefinition(ResourceType.BinaryFile)]
[ResourceRegistration(RegistrationPlatforms.All, EndianMapped = true)]
public class BinaryResource : Resource
{
    public uint DataSize { get; set; }
    public uint DataOffset { get; set; }

    public BinaryResource(uint dataOffset, uint dataSize) : this()
    {
        DataSize = dataSize;
        DataOffset = dataOffset == 0 ? 0x10u : dataOffset;
    }

    public BinaryResource() : base()
    {
        DataOffset = 0x10;
    }

    public BinaryResource(string path, Endian endianness = Endian.Agnostic) : base(path, endianness)
    {
        if (DataOffset == 0)
        {
            DataOffset = 0x10;
        }
    }

    public override void ParseFromStream(ResourceBinaryReader reader, Endian endianness = Endian.Agnostic)
    {
        base.ParseFromStream(reader, endianness);
        
        DataSize = reader.ReadUInt32();
        DataOffset = reader.ReadUInt32();
        
        reader.BaseStream.Seek(DataOffset, SeekOrigin.Begin);
    }
    
    public override void WriteToStream(ResourceBinaryWriter writer, Endian endianness = Endian.Agnostic)
    {
        base.WriteToStream(writer, endianness);

        if (DataOffset < 0x10)
        {
            DataOffset = 0x10;
        }

        writer.Write(DataSize);
        writer.Write(DataOffset);
        writer.Write(new byte[8]);
        writer.BaseStream.Seek(DataOffset, SeekOrigin.Begin);
    }
}
