using Volatility.Extensions;

namespace Volatility.Resources;

// The BinaryFile resource type is a base type used in several
// other resource types, including Splicer, World Painter 2D,
// and Generic RWAC Wave Content.

// Learn More:
// https://burnout.wiki/wiki/Binary_File

public class BinaryResource : Resource
{
    public override ResourceType GetResourceType() => ResourceType.BinaryFile;

    public uint DataSize;
    public uint DataOffset;

    public BinaryResource(uint dataOffset, uint dataSize)
    {
        DataSize = dataSize;
        DataOffset = dataOffset;
    }

    public BinaryResource() : base() { }
    
    public BinaryResource(string path, Endian endianness = Endian.Agnostic) : base(path, endianness) { }

    public override void ParseFromStream(BinaryReader reader, Endian n = Endian.Agnostic)
    {
        base.ParseFromStream(reader, n);
        
        DataSize = reader.ReadUInt32(n);
        DataOffset = reader.ReadUInt32(n);
        
        reader.BaseStream.Seek(DataOffset, SeekOrigin.Begin);
    }
    
    public override void WriteToStream(BinaryWriter writer, Endian n = Endian.Agnostic)
    {
        writer.Write(DataSize, n);
        writer.Write(DataOffset, n);
        writer.Write(new byte[8]);
    }
}