using System.Runtime.InteropServices;

using Volatility.Utilities;

namespace Volatility.Resources;

// The Splicer resource type contains multiple sound assets and presets for
// how those sounds are played. They are typically triggered by in-game actions.
// Splicers begin with a Binary File resource.

// Learn More:
// https://burnout.wiki/wiki/Splicer

public class Splicer : BinaryResource
{
    public override ResourceType GetResourceType() => ResourceType.Splicer;
    
    public List<SPLICE_Data> Splices = new();

    // Only gets populated when parsing from a stream, or when
    // loading referenced sample IDs through LoadDependentSamples
    private List<Sample> _samples = new();

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

        var spliceRefCounts = new List<int>(numSplices);

        if (Splices == null)
            Splices = new List<SPLICE_Data>(numSplices);

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
            Splices.Add(new SPLICE_Data
            {
                NameHash = nameHash,
                SpliceIndex = spliceIdx,
                ESpliceType = eType,
                Volume = vol,
                RND_Pitch = rpitch,
                RND_Vol = rvol,
                SampleRefs = new List<SPLICE_SampleRef>(numRefs)
            });
        }

        int numSampleRefs = spliceRefCounts.Sum();

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

            Console.WriteLine($"Adding sample {i} as {_samples[i].SampleID}");

            data = null;
        }

        reader.BaseStream.Seek(_sampleRefsPtrOffset + DataOffset, SeekOrigin.Begin);

        // Read SampleRefs
        for (int i = 0; i < Splices.Count; i++)
        {
            int count = spliceRefCounts[i];
            var list = Splices[i].SampleRefs;

            for (int j = 0; j < count; j++)
            {
                ushort sampleIdx = reader.ReadUInt16();
                var sr = new SPLICE_SampleRef
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

        var spliceStartIndices = new List<int>(Splices.Count);
        int runningIndex = 0;

        foreach (SPLICE_Data splice in Splices)
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

        long sampleRefsStart = writer.BaseStream.Position;
        foreach (var splice in Splices)
        {
            foreach (var sr in splice.SampleRefs)
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
        DataSize = (uint)(writer.BaseStream.Length - DataOffset);
        long pos = writer.BaseStream.Position;
        writer.BaseStream.Seek(0, SeekOrigin.Begin);
        writer.Write(DataSize);
        writer.BaseStream.Seek(pos, SeekOrigin.Begin);
    }

    public void LoadDependentSamples(bool recurse = false)
    {
        var needed = Splices
            .SelectMany(s => s.SampleRefs.Select(sr => sr.Sample))
            .Distinct()
            .ToList();
        
        string dir = Path.Combine(AppContext.BaseDirectory, "data", "Splicer", "Samples");
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
    public class SPLICE_Data
    {
        public uint NameHash;
        public ushort SpliceIndex;
        public sbyte ESpliceType;
        public float Volume;
        public float RND_Pitch;
        public float RND_Vol;
        public List<SPLICE_SampleRef> SampleRefs { get; set; }
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