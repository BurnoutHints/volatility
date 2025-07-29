using System.Runtime.InteropServices;

namespace Volatility.Resources;

// The Splicer resource type contains multiple sound assets and presets for
// how those sounds are played. They are typically triggered by in-game actions.
// Splicers begin with a Binary File resource.

// Learn More:
// https://burnout.wiki/wiki/Splicer

public class Splicer : BinaryResource
{
    public override ResourceType GetResourceType() => ResourceType.Splicer;
    
    public List<SPLICE_Data> Splices;
    public List<SPLICE_SampleRef> SampleRefs;

    // Only gets populated when parsing from a stream, or when
    // loading referenced sample IDs through LoadDependentSamples. 
    private List<Sample> _samples;

    public override void ParseFromStream(ResourceBinaryReader reader, Endian endianness = Endian.Agnostic)
    {
        base.ParseFromStream(reader, endianness);

        int version =  reader.ReadInt32();
        if (version != 1)
        {
            throw new InvalidDataException("Version mismatch! Version should be 1.");
        }
        
        int pSampleRefTOC = reader.ReadInt32();
        
        int numSplices = reader.ReadInt32();
        if (numSplices <= 0)
        {
            throw new InvalidDataException("No splices in Splicer file!");
        }

        int lowestSampleIndex = new int();

        if (Splices == null)
            Splices = new List<SPLICE_Data>(numSplices);

        // Read Splice Data
        for (int i = 0; i < numSplices; i++)
        {
            // NameHash (null)
            _ = reader.ReadInt32();

            SPLICE_Data spliceData = new SPLICE_Data()
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
            
            spliceData.SampleListIndex = lowestSampleIndex;

            Splices.Add(spliceData);

            lowestSampleIndex += Splices[i].Num_SampleRefs;
        }

        int numSampleRefs = Splices[numSplices - 1].SampleListIndex + Splices[numSplices - 1].Num_SampleRefs;

        long _sampleRefsPtrOffset = reader.BaseStream.Position - DataOffset;

        reader.BaseStream.Seek(pSampleRefTOC + 0xC + DataOffset, SeekOrigin.Begin);

        int numSamples = reader.ReadInt32();

        List<long> _samplePtrs = new List<long>(numSamples);
        for (int i = 0; i < numSamples; i++)
        {
            _samplePtrs.Add(reader.ReadInt32());
        }

        long _samplePtrOffset = reader.BaseStream.Position - DataOffset;

        if (_samples == null)
            _samples = new List<Sample>(numSamples);

        for (int i = 0; i < numSamples; i++)
        {
            reader.BaseStream.Seek(_samplePtrOffset + DataOffset + _samplePtrs[i], SeekOrigin.Begin);
                          
            int length = (int)((i == (numSamples - 1) ? reader.BaseStream.Length : _samplePtrs[i + 1]) - _samplePtrs[i]);

            byte[]? data = reader.ReadBytes(length);

            _samples.Add
            (
                new Sample
                {
                    SampleID = SnrID.HashFromBytes(data),
                    Data = data,
                }
            );

            data = null;
        }

        reader.BaseStream.Seek(_sampleRefsPtrOffset + DataOffset, SeekOrigin.Begin);

        if (SampleRefs == null)
            SampleRefs = new List<SPLICE_SampleRef>(numSampleRefs);

        // Read SampleRefs
        for (int i = 0; i < numSampleRefs; i++)
        {
            int SampleIndex = reader.ReadUInt16();

            SPLICE_SampleRef sampleRef = new SPLICE_SampleRef()
            {
                Sample = _samples[SampleIndex].SampleID,
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

            SampleRefs.Add(sampleRef);
        }

    }

    public override void WriteToStream(EndianAwareBinaryWriter writer, Endian endianness = Endian.Agnostic)
    {
        LoadDependentSamples();

        base.WriteToStream(writer, endianness);

        writer.BaseStream.Position = DataOffset;

        writer.Write(1); // version
       
        int sizeOfSplices = Splices.Count * 0x18; // Size of Splice_Data
        int sizeOfSampleRefs = SampleRefs.Count * 0x2C; // Size of Splice_SampleRef
        int sizedata = sizeOfSplices + sizeOfSampleRefs;

        writer.Write(sizedata); // sizedata/pSampleRefTOC

        writer.Write(Splices.Count); // NumSplices

        var spliceSampleRefPtrPatchPos = new List<long>(Splices.Count);
        int runningSampleRefIndex = 0;
        for (int i = 0; i < Splices.Count; i++)
        {
            var s = Splices[i];

            writer.Write(s.NameHash);
            writer.Write(s.SpliceIndex);
            writer.Write(s.ESpliceType);
            writer.Write(s.Num_SampleRefs);
            writer.Write(s.Volume);
            writer.Write(s.RND_Pitch);
            writer.Write(s.RND_Vol);

            writer.Write(0); // pSampleRefList placeholder

            s.SampleListIndex = runningSampleRefIndex;
            Splices[i] = s;
            runningSampleRefIndex += s.Num_SampleRefs;
        }

        long sampleRefsStart = writer.BaseStream.Position;
        foreach (var r in SampleRefs)
        {
            int idx = _samples.FindIndex(s => s.SampleID.Equals(r.Sample));
            writer.Write((ushort)idx);
            writer.Write(r.ESpliceType);
            writer.Write(r.Padding);
            writer.Write(r.Volume);
            writer.Write(r.Pitch);
            writer.Write(r.Offset);
            writer.Write(r.Az);
            writer.Write(r.Duration);
            writer.Write(r.FadeIn);
            writer.Write(r.FadeOut);
            writer.Write(r.RND_Vol);
            writer.Write(r.RND_Pitch);
            writer.Write(r.Priority);
            writer.Write(r.ERollOffType);
            writer.Write(r.Padding2);
        }

        writer.BaseStream.Position = DataOffset + 0xC + sizedata; // Header + sizedata
        int numSamples = _samples.Count;
        int pSampleRefTOC = (int)(writer.BaseStream.Position - DataOffset);

        writer.Write(numSamples);

        // Reserve space for offsets
        long offsetsStart = writer.BaseStream.Position;
        for (int i = 0; i < numSamples; i++) writer.Write(0);

        int running = 0;
        for (int i = 0; i < numSamples; i++)
        {
            byte[] data = _samples[i].Data;
            writer.Write(data);
            // backfill this sample's offset
            long save = writer.BaseStream.Position;
            writer.Seek((int)(offsetsStart + i * 4), SeekOrigin.Begin);
            writer.Write(running);
            writer.Seek((int)save, SeekOrigin.Begin);
            running += data.Length;
        }

        // Update DataSize
        DataSize = (uint)(writer.BaseStream.Length - 0x10);
        long pos = writer.BaseStream.Position;
        writer.BaseStream.Seek(0, SeekOrigin.Begin);
        writer.Write(DataSize);
        writer.BaseStream.Seek(pos, SeekOrigin.Begin);
    }

    public void LoadDependentSamples(bool recurse = false)
    {
        var needed = SampleRefs.Select(r => r.Sample).Distinct().ToList();
        string dir = Path.Combine(AppContext.BaseDirectory, "data", "Resources", "Splicer", "Samples");
        var files = Directory.GetFiles(dir, "*.snr", recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

        var map = new Dictionary<SnrID, byte[]>(needed.Count);
        foreach (var f in files)
        {
            var data = File.ReadAllBytes(f);
            var id = SnrID.HashFromBytes(data);
            if (!map.ContainsKey(id) && needed.Contains(id))
                map[id] = data;
        }

        foreach (var id in needed)
            if (!map.ContainsKey(id))
                throw new FileNotFoundException($"Missing sample for {id}");
        _samples = needed.Select(id => new Sample { SampleID = id, Data = map[id] }).ToList();
    }

    public List<Sample> GetLoadedSamples()
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
        public SnrID Sample;
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

    public struct Sample
    {
        public SnrID SampleID;
        public byte[] Data;
    }
}