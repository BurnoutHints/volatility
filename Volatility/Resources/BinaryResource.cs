namespace Volatility.Resources;

// The BinaryFile resource type is a base type used in several
// other resource types, including Splicer, World Painter 2D,
// and Generic RWAC Wave Content.

// Learn More:
// https://burnout.wiki/wiki/Binary_File

public class BinaryResource : Resource
{
    public override ResourceType GetResourceType() => ResourceType.BinaryFile;
    
    public uint DataSize { get; set; }
    public uint DataOffset { get; set; }

    public BinaryResource(uint dataOffset, uint dataSize)
    {
        DataSize = dataSize;
        DataOffset = dataOffset;
    }

    public BinaryResource() : base() { }
    
    public BinaryResource(string path, Endian endianness = Endian.Agnostic) : base(path, endianness) { }

    public override void ParseFromStream(ResourceBinaryReader reader, Endian endianness = Endian.Agnostic)
    {
        base.ParseFromStream(reader, endianness);
        
        DataSize = reader.ReadUInt32();
        DataOffset = reader.ReadUInt32();
        
        reader.BaseStream.Seek(DataOffset, SeekOrigin.Begin);
    }
    
    public override void WriteToStream(EndianAwareBinaryWriter writer, Endian endianness = Endian.Agnostic)
    {
        writer.Write(DataSize);
        writer.Write(DataOffset);
        writer.Write(new byte[8]);
    }
}