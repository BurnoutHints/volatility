using System.Runtime.InteropServices;

using static Volatility.Utilities.EnvironmentUtilities;

namespace Volatility.Resources;

// The Splicer resource type contains multiple sound assets and presets for
// how those sounds are played. They are typically triggered by in-game actions.
// Splicers begin with a Binary File resource.
//
// Learn More:
// https://burnout.wiki/wiki/Splicer

[ResourceDefinition(ResourceType.Splicer)]
[ResourceRegistration(RegistrationPlatforms.All, EndianMapped = true)]
public class Splicer : BinaryResource
{
    private const int HeaderSize = 0xC;
    private const int SpliceHeaderSize = 0x18;
    private const int SampleRefSize = 0x2C;

    public List<SpliceData> Splices = [];

    // Only gets populated when parsing from a stream, or when
    // loading referenced sample IDs through LoadDependentSamples
    private List<SpliceSample> _samples = [];

    public override void ParseFromStream(ResourceBinaryReader reader, Endian endianness = Endian.Agnostic)
    {
        base.ParseFromStream(reader, endianness);

        int version = reader.ReadInt32();
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

        long spliceHeadersOffset = reader.BaseStream.Position;
        List<SpliceHeader> spliceHeaders = reader.ParseSection(spliceHeadersOffset, numSplices, SpliceHeader.Read);

        Splices = new(numSplices);
        foreach (SpliceHeader header in spliceHeaders)
        {
            Splices.Add(header.ToSpliceData());
        }

        long sampleRefsOffset = spliceHeadersOffset + (numSplices * SpliceHeaderSize);
        long sampleTableOffset = DataOffset + HeaderSize + sizedata;

        reader.BaseStream.Seek(sampleTableOffset, SeekOrigin.Begin);

        int numSamples = reader.ReadInt32();
        long samplePointersOffset = reader.BaseStream.Position;
        List<int> samplePointers = reader.ParseSection(samplePointersOffset, numSamples, r => r.ReadInt32());
        long sampleDataOffset = samplePointersOffset + (numSamples * sizeof(int));

        _samples = ReadSamples(reader, samplePointers, sampleDataOffset);

        long currentSampleRefOffset = sampleRefsOffset;
        for (int i = 0; i < Splices.Count; i++)
        {
            int count = spliceHeaders[i].SampleRefCount;
            List<SpliceSampleRef> sampleRefs = reader.ParseSection(
                currentSampleRefOffset,
                count,
                r => ReadSampleRef(r, _samples));

            Splices[i].SampleRefs.AddRange(sampleRefs);
            currentSampleRefOffset += count * SampleRefSize;
        }
    }

    public override void WriteToStream(ResourceBinaryWriter writer, Endian endianness = Endian.Agnostic)
    {
        LoadDependentSamples();

        int totalRefs = Splices.Sum(s => s.SampleRefs.Count);
        int sizeOfSplices = Splices.Count * SpliceHeaderSize;
        int sizeOfSampleRefs = totalRefs * SampleRefSize;

        DataSize = (uint)(HeaderSize + sizeOfSplices + sizeOfSampleRefs + sizeof(int));

        base.WriteToStream(writer, endianness);
        writer.BaseStream.Position = DataOffset;

        int sizedata = sizeOfSplices + sizeOfSampleRefs;
        long spliceHeadersOffset = writer.BaseStream.Position + HeaderSize;
        long sampleRefsOffset = spliceHeadersOffset + sizeOfSplices;
        long sampleTableOffset = DataOffset + HeaderSize + sizedata;

        writer.Write(1); // version
        writer.Write(sizedata);
        writer.Write(Splices.Count);

        writer.WriteSection(spliceHeadersOffset, Splices, WriteSpliceHeader);

        writer.BaseStream.Position = sampleRefsOffset;
        foreach (SpliceData splice in Splices)
        {
            foreach (SpliceSampleRef sampleRef in splice.SampleRefs)
            {
                WriteSampleRef(writer, sampleRef, ResolveSampleIndex(sampleRef.Sample));
            }
        }

        writer.BaseStream.Position = sampleTableOffset;
        writer.Write(_samples.Count);

        long samplePointersStart = writer.BaseStream.Position;
        for (int i = 0; i < _samples.Count; i++)
        {
            writer.Write(0);
        }

        int runningOffset = 0;
        for (int i = 0; i < _samples.Count; i++)
        {
            byte[] data = _samples[i].Data;
            writer.Write(data);

            long savePosition = writer.BaseStream.Position;
            writer.BaseStream.Position = samplePointersStart + (i * sizeof(int));
            writer.Write(runningOffset);
            writer.BaseStream.Position = savePosition;

            runningOffset += data.Length;
        }

        DataSize = (uint)(writer.BaseStream.Length - DataOffset);
        long endPosition = writer.BaseStream.Position;
        writer.BaseStream.Position = 0;
        writer.Write(DataSize);
        writer.BaseStream.Position = endPosition;
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
            {
                map[id] = data;
            }
        }

        foreach (SnrID id in needed.Where(id => !map.ContainsKey(id)))
        {
            throw new FileNotFoundException($"Missing sample for {id}");
        }

        _samples = needed.Select(id => new SpliceSample { SampleID = id, Data = map[id] }).ToList();
    }

    public List<SpliceSample> GetLoadedSamples()
    {
        return _samples;
    }

    public Splicer() : base() { }

    public Splicer(string path, Endian endianness = Endian.Agnostic)
        : base(path, endianness) { }

    private static List<SpliceSample> ReadSamples(
        ResourceBinaryReader reader,
        List<int> samplePointers,
        long sampleDataOffset)
    {
        List<SpliceSample> samples = new(samplePointers.Count);

        for (int i = 0; i < samplePointers.Count; i++)
        {
            long start = sampleDataOffset + samplePointers[i];
            long end = i == samplePointers.Count - 1
                ? reader.BaseStream.Length
                : sampleDataOffset + samplePointers[i + 1];

            reader.BaseStream.Seek(start, SeekOrigin.Begin);
            byte[] data = reader.ReadBytes((int)(end - start));

            samples.Add(new SpliceSample
            {
                SampleID = SnrID.HashFromBytes(data),
                Data = data,
            });
        }

        return samples;
    }

    private static SpliceSampleRef ReadSampleRef(ResourceBinaryReader reader, List<SpliceSample> samples)
    {
        ushort sampleIndex = reader.ReadUInt16();
        return new SpliceSampleRef
        {
            Sample = samples[sampleIndex].SampleID,
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
    }

    private static void WriteSpliceHeader(ResourceBinaryWriter writer, SpliceData splice)
    {
        writer.Write(splice.NameHash);
        writer.Write(splice.SpliceIndex);
        writer.Write(splice.ESpliceType);
        writer.Write((byte)splice.SampleRefs.Count);
        writer.Write(splice.Volume);
        writer.Write(splice.RND_Pitch);
        writer.Write(splice.RND_Vol);
        writer.Write(0);
    }

    private static void WriteSampleRef(ResourceBinaryWriter writer, SpliceSampleRef sampleRef, int sampleIndex)
    {
        writer.Write((ushort)sampleIndex);
        writer.Write(sampleRef.ESpliceType);
        writer.Write(sampleRef.Padding);
        writer.Write(sampleRef.Volume);
        writer.Write(sampleRef.Pitch);
        writer.Write(sampleRef.Offset);
        writer.Write(sampleRef.Az);
        writer.Write(sampleRef.Duration);
        writer.Write(sampleRef.FadeIn);
        writer.Write(sampleRef.FadeOut);
        writer.Write(sampleRef.RND_Vol);
        writer.Write(sampleRef.RND_Pitch);
        writer.Write(sampleRef.Priority);
        writer.Write(sampleRef.ERollOffType);
        writer.Write(sampleRef.Padding2);
    }

    private int ResolveSampleIndex(SnrID sampleId)
    {
        int sampleIndex = _samples.FindIndex(x => x.SampleID == sampleId);
        if (sampleIndex < 0)
        {
            throw new InvalidDataException($"Unable to resolve sample {sampleId} in splicer export.");
        }

        return sampleIndex;
    }

    private readonly record struct SpliceHeader(
        uint NameHash,
        ushort SpliceIndex,
        sbyte ESpliceType,
        byte SampleRefCount,
        float Volume,
        float RandomPitch,
        float RandomVolume)
    {
        public static SpliceHeader Read(ResourceBinaryReader reader)
        {
            SpliceHeader header = new(
                reader.ReadUInt32(),
                reader.ReadUInt16(),
                reader.ReadSByte(),
                reader.ReadByte(),
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle());

            _ = reader.ReadInt32();
            return header;
        }

        public SpliceData ToSpliceData()
        {
            return new SpliceData
            {
                NameHash = NameHash,
                SpliceIndex = SpliceIndex,
                ESpliceType = ESpliceType,
                Volume = Volume,
                RND_Pitch = RandomPitch,
                RND_Vol = RandomVolume,
                SampleRefs = new List<SpliceSampleRef>(SampleRefCount)
            };
        }
    }

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
