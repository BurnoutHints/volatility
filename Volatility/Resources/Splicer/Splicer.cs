using System.Runtime.InteropServices;
using System.Text;

namespace Volatility.Resources;

// The Splicer resource type contains multiple sound assets and presets for
// how those sounds are played. They are typically triggered by in-game actions.
// Splicers begin with a Binary File resource.

// Learn More:
// https://burnout.wiki/wiki/Splicer

public class Splicer : BinaryResource
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
    [EditorReadOnly]
    public IntPtr SampleRefsPtrOffset;
    
    [EditorReadOnly]
    public IntPtr SamplePtrOffset;


    public override void ParseFromStream(ResourceBinaryReader reader, Endian endianness = Endian.Agnostic)
    {
        base.ParseFromStream(reader, endianness);

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

    public override void WriteToStream(EndianAwareBinaryWriter writer, Endian endianness = Endian.Agnostic)
    {
        base.WriteToStream(writer, endianness);

        writer.BaseStream.Position = DataOffset;

        writer.Write((int)1); // version

        int sampleRefTOCPosition = (int)writer.BaseStream.Position; // Saving this for later

        writer.Write((int)0); // pSampleRefTOC

        writer.Write((int)Splices.Length); // NumSplices

        // Write Splices
        for (int i = 0; i < Splices.Length; i++)
        {
            // NameHash, unused by game
            writer.Write(Encoding.Default.GetBytes("sper"));

            writer.Write(Splices[i].SpliceIndex);
            writer.Write(Splices[i].ESpliceType);
            writer.Write(Splices[i].Num_SampleRefs);
            writer.Write(Splices[i].Volume);
            writer.Write(Splices[i].RND_Pitch);
            writer.Write(Splices[i].RND_Vol);

            // pSampleRefList, filled at runtime
            writer.Write(Encoding.Default.GetBytes("dunk"));
        }

        // Write SampleRefs
        for (int i = 0; i < SampleRefs.Length; i++)
        {
            writer.Write(SampleRefs[i].SampleIndex);
            writer.Write(SampleRefs[i].ESpliceType);
            writer.Write(SampleRefs[i].Padding);
            writer.Write(SampleRefs[i].Volume);
            writer.Write(SampleRefs[i].Pitch);
            writer.Write(SampleRefs[i].Offset);
            writer.Write(SampleRefs[i].Az);
            writer.Write(SampleRefs[i].Duration);
            writer.Write(SampleRefs[i].FadeIn);
            writer.Write(SampleRefs[i].FadeOut);
            writer.Write(SampleRefs[i].RND_Vol);
            writer.Write(SampleRefs[i].RND_Pitch);
            writer.Write(SampleRefs[i].Priority);
            writer.Write(SampleRefs[i].ERollOffType);
            writer.Write(SampleRefs[i].Padding2);
        }

        int sampleRefTOC = ((int)writer.BaseStream.Position) - (int)DataOffset; // Saving this for later

        writer.Seek(sampleRefTOCPosition, SeekOrigin.Begin);

        writer.Write(sampleRefTOC);

        writer.Seek(sampleRefTOCPosition + (int)DataOffset, SeekOrigin.Begin);
    }

    public void SpliceSamples(EndianAwareBinaryWriter writer, string samplesDir)
    {
        // Enumerate then write Samples
        string samplesDirectory = Path.Combine(Path.GetDirectoryName(samplesDir), $"{AssetName}_Samples");

        string[] paths = Directory.GetFiles(samplesDirectory, "*.snr");
        byte[][] samples = Array.Empty<byte[]>();
        int[] lengths = Array.Empty<int>();

        int lowestIndex = 0;

        for (int i = 0; i < paths.Length; i++)
        {
            samples[i] = File.ReadAllBytes(paths[i]);
            lengths[i] = samples[i].Length;

            // Write SamplePtrs
            writer.Write(lowestIndex);

            lowestIndex += samples[i].Length;
        }

        for (int i = 0; i < samples.Length; i++)
        {
            writer.Write(samples[i]);
        }

        long tempOffset = writer.BaseStream.Position;

        // Handle the BinaryResource data size
        // Not exactly a fan of how this is hardcoded.
        DataSize = (uint)(writer.BaseStream.Length - 0x10);
        writer.BaseStream.Seek(0, SeekOrigin.Begin);
        writer.Write(DataSize);

        writer.BaseStream.Seek(tempOffset, SeekOrigin.Begin);
    }

    public byte[][] GetLoadedSamples()
    {
        return _samples;
    }

    public Splicer() : base() { }

    public Splicer(string path, Endian endianness = Endian.Agnostic) : base(path, endianness) { }
    
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