using Volatility.Utilities;

namespace Volatility.Resources;

[ResourceDefinition(ResourceType.EnvironmentTimeLine)]
[ResourceRegistration(RegistrationPlatforms.All, EndianMapped = true)]
public class EnvironmentTimeline : Resource
{
    private const int SectionAlignment = 0x10;
    private const int KeyframeTimeSize = sizeof(float);
    private const int KeyframeReferencePlaceholderSize = sizeof(uint);

    public LocationData[] Locations = [];

    public override void WriteToStream(ResourceBinaryWriter writer, Endian endianness = Endian.Agnostic)
    {
        base.WriteToStream(writer, endianness);

        LocationData[] locations = Locations ?? [];

        long currentOffset = 0x10;  // Header size
        ulong locationsOffset = ResourceUtilities.GetSectionOffset(
            ref currentOffset,
            locations.Length,
            GetLocationStructSize(ResourceArch),
            SectionAlignment);

        ulong[] keyframeTimesOffsets = new ulong[locations.Length];
        ulong[] keyframeRefsOffsets = new ulong[locations.Length];

        int totalImports = 0;
        for (int i = 0; i < locations.Length; i++)
        {
            KeyframeReference[] keyframes = locations[i].Keyframes ?? [];

            keyframeTimesOffsets[i] = ResourceUtilities.GetSectionOffset(
                ref currentOffset,
                keyframes.Length,
                KeyframeTimeSize,
                SectionAlignment);

            keyframeRefsOffsets[i] = ResourceUtilities.GetSectionOffset(
                ref currentOffset,
                keyframes.Length,
                KeyframeReferencePlaceholderSize,
                SectionAlignment);

            totalImports += keyframes.Length;
        }

        long importsOffset = ResourceUtilities.GetSectionOffset(
            ref currentOffset,
            totalImports * ResourceImport.ImportEntrySize,
            SectionAlignment);

        writer.Write(0x1);                      // Version
        writer.Write(locations.Length);
        writer.Write((uint)locationsOffset);    // Locations Pointer
        writer.Write(0x0);                      // Padding

        writer.WriteSection(locationsOffset, locations, (w, location, index) =>
            WriteLocationHeader(w, location, ResourceArch, keyframeTimesOffsets[index], keyframeRefsOffsets[index]));

        for (int i = 0; i < locations.Length; i++)
        {
            KeyframeReference[] keyframes = locations[i].Keyframes ?? [];

            writer.WriteSection<KeyframeReference>(keyframeTimesOffsets[i], keyframes, static (w, keyframe) => w.Write(keyframe.KeyframeTime));
            writer.WriteSection<KeyframeReference>(keyframeRefsOffsets[i], keyframes, static (w, _) => w.Write(0u));
        }

        if (importsOffset != 0)
        {
            writer.BaseStream.Position = importsOffset;
            for (int i = 0; i < locations.Length; i++)
            {
                KeyframeReference[] keyframes = locations[i].Keyframes ?? [];
                for (int j = 0; j < keyframes.Length; j++)
                {
                    writer.Write(keyframes[j].ResourceReference.ReferenceID);
                    writer.Write((uint)(keyframeRefsOffsets[i] + ((ulong)j * KeyframeReferencePlaceholderSize)));
                    writer.Write(0u);
                }
            }
        }
    }

    public override void ParseFromStream(ResourceBinaryReader reader, Endian endianness = Endian.Agnostic)
    {
        base.ParseFromStream(reader, endianness);

        int version = reader.ReadInt32();
        if (version != 1)
        {
            throw new InvalidDataException($"Version mismatch! Version should be 1. (Found version {version})");
        }

        int locationCount = reader.ReadInt32();
        ulong locationsPtr = reader.ReadPointer(ResourceArch);
        reader.BaseStream.Seek(0x10, SeekOrigin.Begin);

        Locations = reader.ParseSection(locationsPtr, locationCount, r => ReadLocation(r, ResourceArch)).ToArray();
    }

    public EnvironmentTimeline() : base() { }

    public EnvironmentTimeline(string path, Endian endianness = Endian.Agnostic) : base(path, endianness) { }

    private static int GetLocationStructSize(Arch arch)
    {
        return arch == Arch.x64 ? 0x18 : 0x0C;
    }

    private static LocationData ReadLocation(ResourceBinaryReader reader, Arch arch)
    {
        uint keyframeCount = reader.ReadArchDependUInt(arch);
        ulong keyframeTimesPtr = reader.ReadPointer(arch);
        ulong keyframeRefsPtr = reader.ReadPointer(arch);

        KeyframeReference[] keyframes = new KeyframeReference[keyframeCount];
        if (keyframeCount == 0)
        {
            return new LocationData { Keyframes = keyframes };
        }

        List<float> keyframeTimes = reader.ParseSection(keyframeTimesPtr, (int)keyframeCount, r => r.ReadSingle());

        long importBlockOffset = Math.Max(
            (long)keyframeTimesPtr + (keyframeCount * KeyframeTimeSize),
            (long)keyframeRefsPtr + (keyframeCount * KeyframeReferencePlaceholderSize));

        for (int i = 0; i < keyframeCount; i++)
        {
            long fileOffset = (long)keyframeRefsPtr + (i * KeyframeReferencePlaceholderSize);
            ResourceImport.ReadExternalImport(fileOffset, reader, importBlockOffset, out ResourceImport resourceReference);

            keyframes[i] = new KeyframeReference
            {
                KeyframeTime = i < keyframeTimes.Count ? keyframeTimes[i] : 0f,
                ResourceReference = resourceReference
            };
        }

        return new LocationData
        {
            Keyframes = keyframes
        };
    }

    private static void WriteLocationHeader(
        ResourceBinaryWriter writer,
        LocationData location,
        Arch arch,
        ulong keyframeTimesOffset,
        ulong keyframeRefsOffset)
    {
        uint keyframeCount = (uint)(location.Keyframes?.Length ?? 0);
        writer.WriteArchDependUInt(keyframeCount, arch);
        writer.WritePointer(keyframeTimesOffset, arch);
        writer.WritePointer(keyframeRefsOffset, arch);
    }

    public struct LocationData
    {
        public KeyframeReference[] Keyframes;
    }

    public struct KeyframeReference
    {
        public float KeyframeTime;
        public ResourceImport ResourceReference;
    }
}
