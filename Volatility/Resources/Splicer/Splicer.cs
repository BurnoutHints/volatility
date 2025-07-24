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
    
    public List<SPLICE_Data> Splices;
    public List<SPLICE_SampleRef> SampleRefs;
    public IntPtr[] SamplePtrs;

    // This only gets populated when parsing from a stream.
    // Not sure whether this is a good idea to keep as-is.
    private List<Sample> _samples;

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

        SampleRefsPtrOffset = (nint)(reader.BaseStream.Position - DataOffset);

        reader.BaseStream.Seek(pSampleRefTOC + 0xC + DataOffset, SeekOrigin.Begin);

        int numSamples = reader.ReadInt32();

        SamplePtrs = new IntPtr[numSamples];
        for (int i = 0; i < numSamples; i++)
        {
            SamplePtrs[i] = reader.ReadInt32();
        }

        SamplePtrOffset = (nint)(reader.BaseStream.Position - DataOffset);

        if (_samples == null)
            _samples = new List<Sample>(numSamples);

        for (int i = 0; i < numSamples; i++)
        {
            reader.BaseStream.Seek(SamplePtrOffset + DataOffset + SamplePtrs[i], SeekOrigin.Begin);
                          
            int length = (int)((i == (numSamples - 1) ? reader.BaseStream.Length : SamplePtrs[i + 1]) - SamplePtrs[i]);

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

        reader.BaseStream.Seek(SampleRefsPtrOffset + DataOffset, SeekOrigin.Begin);

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

        long pSampleRefTocFieldPos = writer.BaseStream.Position;  // Saving this for later
        writer.Write(0); // pSampleRefTOC placeholder

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

            spliceSampleRefPtrPatchPos.Add(writer.BaseStream.Position);
            writer.Write(0); // pSampleRefList placeholder

            s.SampleListIndex = runningSampleRefIndex;
            Splices[i] = s;
            runningSampleRefIndex += s.Num_SampleRefs;
        }

        long sampleRefsStart = writer.BaseStream.Position;
        for (int i = 0; i < SampleRefs.Count; i++)
        {
            var r = SampleRefs[i];
            int sampleIndex = _samples.FindIndex(s => s.SampleID.Equals(r.Sample));

            writer.Write((ushort)sampleIndex);
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
        long sampleRefsEnd = writer.BaseStream.Position;

        // Backpatch each splice's pSampleRefList
        for (int i = 0; i < Splices.Count; i++)
        {
            long patchPos = spliceSampleRefPtrPatchPos[i];
            int offset = (int)(sampleRefsStart - DataOffset + Splices[i].SampleListIndex * Marshal.SizeOf<SPLICE_SampleRef>());
            writer.Seek((int)patchPos, SeekOrigin.Begin);
            writer.Write(offset);
        }
        writer.Seek((int)sampleRefsEnd, SeekOrigin.Begin);

        int numSamples = _samples.Count;
        int pSampleRefTOC = (int)(writer.BaseStream.Position - DataOffset);

        writer.Write(numSamples);

        // Reserve space for offsets
        long offsetsStart = writer.BaseStream.Position;
        for (int i = 0; i < numSamples; i++) writer.Write(0);

        long dataStart = writer.BaseStream.Position;
        int running = 0;
        for (int i = 0; i < numSamples; i++)
        {
            byte[] data = _samples[i].Data;
            writer.Write(data);
            long curPos = writer.BaseStream.Position;
            // backfill this sample's offset
            long save = writer.BaseStream.Position;
            writer.Seek((int)(offsetsStart + i * 4), SeekOrigin.Begin);
            writer.Write(running);
            writer.Seek((int)save, SeekOrigin.Begin);
            running += data.Length;
        }

        // Backfill pSampleRefTOC in header
        long endPos = writer.BaseStream.Position;
        writer.Seek((int)pSampleRefTocFieldPos, SeekOrigin.Begin);
        writer.Write(pSampleRefTOC);
        writer.Seek((int)endPos, SeekOrigin.Begin);

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

        _samples = new List<Sample>(needed.Count);
        SamplePtrs = new IntPtr[needed.Count];

        int running = 0;
        for (int i = 0; i < needed.Count; i++)
        {
            var data = map[needed[i]];
            _samples.Add(new Sample { SampleID = needed[i], Data = data });
            SamplePtrs[i] = running;
            running += data.Length;
        }
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