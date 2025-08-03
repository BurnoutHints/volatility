using System.Runtime.InteropServices;

using static Volatility.Utilities.EnvironmentUtilities;

namespace Volatility.Resources;

// The Splicer resource type contains multiple sound assets and presets for
// how those sounds are played. They are typically triggered by in-game actions.
// Splicers begin with a Binary File resource.

// Learn More:
// https://burnout.wiki/wiki/Splicer

public class Splicer : BinaryResource
{
    public override ResourceType GetResourceType() => ResourceType.Splicer;
    
    public List<SpliceData> Splices = [];

    // Only gets populated when parsing from a stream, or when
    // loading referenced sample IDs through LoadDependentSamples
    private List<SpliceSample> _samples = [];

    public override void ParseFromStream(ResourceBinaryReader reader, Endian endianness = Endian.Agnostic)
    {
        base.ParseFromStream(reader, endianness);

        int version =  reader.ReadInt32();
        if (version != 1)
        {
            throw new InvalidDataException("Version mismatch! Version should be 1.");
        }
        
        int sizedata = reader.ReadInt32();
        
        int numSplices = reader.ReadInt32();
        if (numSplices <= 0)
        {
            throw new InvalidDataException("No splices in Splicer file!");
        }

        List<int> spliceRefCounts = new(numSplices);

        // Read Splice Data
        for (int i = 0; i < numSplices; i++)
        {
            uint nameHash = reader.ReadUInt32();
            ushort spliceIdx = reader.ReadUInt16();
            sbyte eType = reader.ReadSByte();
            byte numRefs = reader.ReadByte();
            float vol = reader.ReadSingle();
            float rpitch = reader.ReadSingle();
            float rvol = reader.ReadSingle();
            _ = reader.ReadInt32();    // pSampleRefList (null)

            spliceRefCounts.Add(numRefs);
            Splices.Add(new SpliceData
            {
                NameHash = nameHash,
                SpliceIndex = spliceIdx,
                ESpliceType = eType,
                Volume = vol,
                RND_Pitch = rpitch,
                RND_Vol = rvol,
                SampleRefs = new List<SpliceSampleRef>(numRefs)
            });
        }
        
        long sampleRefsPtrOffset = reader.BaseStream.Position - DataOffset;

        reader.BaseStream.Seek(sizedata + 0xC + DataOffset, SeekOrigin.Begin);

        int numSamples = reader.ReadInt32();

        List<long> samplePtrs = new(numSamples);
        for (int i = 0; i < numSamples; i++)
        {
            samplePtrs.Add(reader.ReadInt32());
        }

        long samplePtrOffset = reader.BaseStream.Position - DataOffset;

        for (int i = 0; i < numSamples; i++)
        {
            reader.BaseStream.Seek(samplePtrOffset + DataOffset + samplePtrs[i], SeekOrigin.Begin);

            int length = (int)((i == (numSamples - 1) ? reader.BaseStream.Length : samplePtrs[i + 1]) - samplePtrs[i]);

            byte[]? data = reader.ReadBytes(length);

            _samples.Add
            (
                new SpliceSample
                {
                    SampleID = SnrID.HashFromBytes(data),
                    Data = data,
                }
            );

            Console.WriteLine($"Adding sample {i} as {_samples[i].SampleID}");
        }

        reader.BaseStream.Seek(sampleRefsPtrOffset + DataOffset, SeekOrigin.Begin);

        // Read SampleRefs
        for (int i = 0; i < Splices.Count; i++)
        {
            int count = spliceRefCounts[i];
            List<SpliceSampleRef> list = Splices[i].SampleRefs;

            for (int j = 0; j < count; j++)
            {
                ushort sampleIdx = reader.ReadUInt16();
                SpliceSampleRef sr = new()
                {
                    Sample = _samples[sampleIdx].SampleID,
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
                    Padding2 = reader.ReadUInt16()
                };
                list.Add(sr);
            }
        }
    }

    public override void WriteToStream(EndianAwareBinaryWriter writer, Endian endianness = Endian.Agnostic)
    {
        LoadDependentSamples();

        base.WriteToStream(writer, endianness);

        writer.BaseStream.Position = DataOffset;

        writer.Write(1); // version

        int totalRefs = Splices.Sum(s => s.SampleRefs.Count);
        int sizeOfSplices = Splices.Count * 0x18; // Size of Splice_Data
        int sizeOfSampleRefs = totalRefs * 0x2C; // Size of Splice_SampleRef
        int sizedata = sizeOfSplices + sizeOfSampleRefs;

        writer.Write(sizedata); // sizedata/pSampleRefTOC

        writer.Write(Splices.Count); // NumSplices
        
        foreach (SpliceData splice in Splices)
        {
            writer.Write(splice.NameHash);
            writer.Write(splice.SpliceIndex);
            writer.Write(splice.ESpliceType);
            writer.Write((byte)splice.SampleRefs.Count);
            writer.Write(splice.Volume);
            writer.Write(splice.RND_Pitch);
            writer.Write(splice.RND_Vol);

            writer.Write(0); // pSampleRefList placeholder
        }

        foreach (SpliceData splice in Splices)
        {
            foreach (SpliceSampleRef sr in splice.SampleRefs)
            {
                int sampleIdx = _samples.FindIndex(x => x.SampleID == sr.Sample);
                writer.Write((ushort)sampleIdx);
                writer.Write(sr.ESpliceType);
                writer.Write(sr.Padding);
                writer.Write(sr.Volume);
                writer.Write(sr.Pitch);
                writer.Write(sr.Offset);
                writer.Write(sr.Az);
                writer.Write(sr.Duration);
                writer.Write(sr.FadeIn);
                writer.Write(sr.FadeOut);
                writer.Write(sr.RND_Vol);
                writer.Write(sr.RND_Pitch);
                writer.Write(sr.Priority);
                writer.Write(sr.ERollOffType);
                writer.Write(sr.Padding2);
            }
        }

        writer.BaseStream.Position = DataOffset + 0xC + sizedata; // Header + sizedata
        int numSamples = _samples.Count;

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
        DataSize = (uint)(writer.BaseStream.Length - DataOffset);
        long pos = writer.BaseStream.Position;
        writer.BaseStream.Seek(0, SeekOrigin.Begin);
        writer.Write(DataSize);
        writer.BaseStream.Seek(pos, SeekOrigin.Begin);
    }

    public void LoadDependentSamples(bool recurse = false)
    {
        List<SnrID> needed = Splices
            .SelectMany(s => s.SampleRefs.Select(sr => sr.Sample))
            .Distinct()
            .ToList();
        
        string dir = Path.Combine
        (
            GetEnvironmentDirectory(EnvironmentDirectory.Splicer),  
            "Samples"
        );

        string[] files = Directory.GetFiles(dir, "*.snr", recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

        Dictionary<SnrID, byte[]> map = new(needed.Count);
        foreach (string f in files)
        {
            byte[] data = File.ReadAllBytes(f);
            SnrID id = SnrID.HashFromBytes(data);
            if (!map.ContainsKey(id) && needed.Contains(id))
                map[id] = data;
        }

        foreach (SnrID id in needed.Where(id => !map.ContainsKey(id)))
            throw new FileNotFoundException($"Missing sample for {id}");
        
        _samples = needed.Select(id => new SpliceSample { SampleID = id, Data = map[id] }).ToList();
    }

    public List<SpliceSample> GetLoadedSamples()
    {
        return _samples;
    }

    public Splicer() : base() { }

    public Splicer(string path, Endian endianness = Endian.Agnostic) : base(path, endianness) { }
    
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class SpliceData
    {
        public uint NameHash;
        public ushort SpliceIndex;
        public sbyte ESpliceType;
        public float Volume;
        public float RND_Pitch;
        public float RND_Vol;
        public required List<SpliceSampleRef> SampleRefs { get; set; }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SpliceSampleRef
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

    public struct SpliceSample
    {
        public SnrID SampleID;
        public byte[] Data;
    }
}