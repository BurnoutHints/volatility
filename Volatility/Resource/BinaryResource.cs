using static Volatility.Utilities.DataUtilities;

namespace Volatility.Resource;

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

    public BinaryResource() { }
    
    public BinaryResource(string path) : base(path) { }

    public override void ParseFromStream(BinaryReader reader)
    {
        base.ParseFromStream(reader);

        bool be = GetResourceEndian() == Endian.BE;
        
        DataSize = be ? SwapEndian(reader.ReadUInt32()) : reader.ReadUInt32();
        DataOffset = be ? SwapEndian(reader.ReadUInt32()) : reader.ReadUInt32();
        
        reader.BaseStream.Seek(DataOffset, SeekOrigin.Begin);
    }
}