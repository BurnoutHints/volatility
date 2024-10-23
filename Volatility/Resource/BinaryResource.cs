namespace Volatility.Resource;

// The BinaryFile resource type is a base type used in several
// other resource types, including Splicer, World Painter 2D,
// and Generic RWAC Wave Content.

// Learn More:
// https://burnout.wiki/wiki/Binary_File

public class BinaryResource : Resource
{
    public new static readonly ResourceType ResourceType = ResourceType.BinaryFile;

    uint DataSize;
    uint DataOffset;

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
        
        DataSize = reader.ReadUInt32();
        DataOffset = reader.ReadUInt32();
        
        reader.BaseStream.Seek(DataOffset, SeekOrigin.Begin);
    }
}