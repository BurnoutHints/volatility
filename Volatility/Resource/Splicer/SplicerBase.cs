using System.Runtime.InteropServices;

namespace Volatility.Resource.Splicer;

public abstract class SplicerBase : BinaryResource
{
    public override ResourceType GetResourceType() => ResourceType.Splicer;
    
    public SPLICE_Data[] Splices;
    public SPLICE_SampleRef[] SampleRefs;
    public IntPtr[] SamplePtrs;

    // This only gets populated when parsing from a stream.
    // Not sure whether this is a good idea to keep as-is.
    private byte[][] _samples;

    // Used to make reading parsed files easier.
    // May remove or keep as generated values
    // to make reading & editing easier.
    public IntPtr SampleRefsPtrOffset;
    public IntPtr SamplePtrOffset;


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

        int numSampleRefs = Splices[numSplices - 1].SampleListIndex + Splices[numSplices - 1].Num_SampleRefs;

        SampleRefsPtrOffset = (nint)(reader.BaseStream.Position - DataOffset);

        // Read SampleRefs
        SampleRefs = new SPLICE_SampleRef[numSampleRefs];
        for (int i = 0; i < numSampleRefs; i++)
        {
            SampleRefs[i] = new SPLICE_SampleRef()
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

        reader.BaseStream.Seek(pSampleRefTOC + 0xC + DataOffset, SeekOrigin.Begin);

        int numSamples = reader.ReadInt32();

        SamplePtrs = new IntPtr[numSamples];
        for (int i = 0; i < numSamples; i++)
        {
            SamplePtrs[i] = reader.ReadInt32();
        }

        SamplePtrOffset = (nint)(reader.BaseStream.Position - DataOffset);

        _samples = new byte[numSamples][];
        for (int i = 0; i < numSamples; i++)
        {
            reader.BaseStream.Seek(SamplePtrOffset + DataOffset + SamplePtrs[i], SeekOrigin.Begin);
                          
            int length = (int)((i == (numSamples - 1) ? reader.BaseStream.Length : SamplePtrs[i + 1]) - SamplePtrs[i]);

            _samples[i] = reader.ReadBytes(length);
        }
    }

    public byte[][] GetLoadedSamples()
    {
        return _samples;
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