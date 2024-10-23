namespace Volatility.Resource.Splicer;
using System.Runtime.InteropServices;

public abstract class SplicerBase : BinaryResource
{
    public new static readonly ResourceType ResourceType = ResourceType.Splicer;
    
    public int Version;
    public int mpTableOfContents;
    public int mNumSplices;

    public override void ParseFromStream(BinaryReader reader)
    {
        base.ParseFromStream(reader);
        
        
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SPLICE_Data
    {
        public uint NameHash;
        public ushort SpliceIndex;
        public sbyte ESpliceType;
        public byte Num_SampleRefs;
        public float Volume;
        public float RND_Pitch;
        public float RND_Vol;
        public int pSampleRefList;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SPLICE_SampleRef
    {
        public ushort SampleIndex;
        public sbyte ESpliceType;
        public byte Padding;
        public float Volume;
        public float Pitch;
        public float Offset;
        public float Az;
        public float Duration;
        public float FadeIn;
        public float FadeOut;
        public float RND_Vol;
        public float RND_Pitch;
        public byte Priority;
        public byte ERollOffType;
        public ushort Padding2;
    }
}