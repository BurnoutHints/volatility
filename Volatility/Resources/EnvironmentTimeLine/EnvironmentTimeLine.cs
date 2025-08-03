using YamlDotNet.Serialization;

namespace Volatility.Resources;

public class EnvironmentTimeline : Resource
{
    public override ResourceType GetResourceType() => ResourceType.EnvironmentTimeLine;

    public LocationData[] Locations;

    public override void WriteToStream(EndianAwareBinaryWriter writer, Endian endianness = Endian.Agnostic)
    {
        base.WriteToStream(writer, endianness);

        writer.Write(0x1);              // Version
        writer.Write(Locations.Length);
        writer.Write(0x10);             // Locations Pointer
        writer.Write(0x0);              // Padding
    }

    public override void ParseFromStream(ResourceBinaryReader reader, Endian endianness = Endian.Agnostic)
    {
        base.ParseFromStream(reader, endianness);

        Arch arch = GetResourceArch();

        int version = reader.ReadInt32();
        if (version != 1)
        {
            throw new InvalidDataException($"Version mismatch! Version should be 1. (Found version {version})");
        }

        uint locationCount = reader.ReadUInt32();
        Locations = new LocationData[locationCount];

        uint locationsPtr = reader.ReadUInt32();

        for (int i = 0; i < locationCount; i++) 
        {
            reader.BaseStream.Seek(locationsPtr + ((arch == Arch.x64 ? 0x18 : 0xC) * i), SeekOrigin.Begin);
            uint keyframeCount = (uint)(arch == Arch.x64 ? reader.ReadUInt64() : reader.ReadUInt32());
            ulong keyframeTimesPtr = (arch == Arch.x64 ? reader.ReadUInt64() : reader.ReadUInt32());
            ulong keyframeRefsPtr = (arch == Arch.x64 ? reader.ReadUInt64() : reader.ReadUInt32());

            Locations[i].Keyframes = new KeyframeReference[keyframeCount];

            long maxLength = (long)new[]
            {
                keyframeTimesPtr + (keyframeCount * sizeof(uint)),
                keyframeRefsPtr + (keyframeCount * sizeof(uint)),
            }.Max();

            for (int j = 0; j < keyframeCount; j++)
            {
                reader.BaseStream.Seek((long)keyframeTimesPtr + (0x4 * j), SeekOrigin.Begin);

                Locations[i].Keyframes[j].KeyframeTime = reader.ReadSingle();

                reader.BaseStream.Seek((long)keyframeRefsPtr + (0x4 * j), SeekOrigin.Begin);

                ResourceImport.ReadExternalImport(fileOffset: reader.BaseStream.Position, reader, maxLength, out Locations[i].Keyframes[j].ResourceReference);
            }
        }
    }

    public EnvironmentTimeline() : base() { }

    public EnvironmentTimeline(string path, Endian endianness = Endian.Agnostic) : base(path, endianness) { }

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
