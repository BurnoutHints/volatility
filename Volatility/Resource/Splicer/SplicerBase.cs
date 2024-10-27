using System.Runtime.InteropServices;

namespace Volatility.Resource.Splicer;

public abstract class SplicerBase : BinaryResource
{
    public override ResourceType GetResourceType() => ResourceType.Splicer;
    
    public SPLICE_Data[] Splices;
    public SPLICE_SampleRef[] Samples;

    public override void ParseFromStream(EndianAwareBinaryReader reader)
    {
        base.ParseFromStream(reader);

        int version =  reader.ReadInt32();
        if (version != 1)
        {
            throw new InvalidDataException("Version mismatch! Version should be 1.");
            return;
        }
        
        int pSampleRefTOC = reader.ReadInt32();
        
        int numSplices = reader.ReadInt32();
        if (numSplices <= 0)
        {
            throw new InvalidDataException("No splices in Splicer file!");
            return;
        }

        int lowestSampleIndex = new int();
        // Read Splice Data
        Splices = new SPLICE_Data[numSplices];
        for (int i = 0; i < numSplices; i++)
        {
            // NameHash (null)
            _ = reader.ReadInt32();
            
            Splices[i] = new SPLICE_Data()
            {
                SpliceIndex = reader.ReadUInt16(),
                ESpliceType = reader.ReadSByte(),
                Num_SampleRefs = reader.ReadByte(),
                Volume = reader.ReadSingle(),
                RND_Pitch = reader.ReadSingle(),
                RND_Vol = reader.ReadSingle(),  
            };
            
            // pSampleRefList (null)
            _ = reader.ReadInt32();
            
            Splices[i].SampleListIndex = lowestSampleIndex;
            lowestSampleIndex += Splices[i].Num_SampleRefs;
        }
        
        // Read SampleRefs
        reader.BaseStream.Seek(pSampleRefTOC + 0xC + DataOffset, SeekOrigin.Begin);
        
        int numSamples = reader.ReadInt32();
        
        Samples = new SPLICE_SampleRef[numSamples];
        for (int i = 0; i < numSamples; i++)
        {
            Samples[i] = new SPLICE_SampleRef()
            {
                SampleIndex = reader.ReadUInt16(),
                ESpliceType = reader.ReadSByte(),
                Padding = reader.ReadByte(),
                Volume = reader.ReadSingle(),
                Pitch = reader.ReadSingle(),
                Offset = reader.ReadSingle(),
                Az = reader.ReadSingle(),
                Duration = reader.ReadSingle(),
                FadeIn = reader.ReadSingle(),
                FadeOut = reader.ReadSingle(),
                RND_Vol = reader.ReadSingle(),
                RND_Pitch = reader.ReadSingle(),
                Priority = reader.ReadByte(),
                ERollOffType = reader.ReadByte(),
                Padding2 = reader.ReadUInt16(),
            };
        }
    }

    public SplicerBase() : base() { }

    public SplicerBase(string path) : base(path) { }
    
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
        public int SampleListIndex;
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