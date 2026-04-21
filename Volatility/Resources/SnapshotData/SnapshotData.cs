namespace Volatility.Resources;

[ResourceDefinition(ResourceType.SnapshotData)]
[ResourceRegistration(RegistrationPlatforms.All, EndianMapped = true)]
public class SnapshotData : BinaryResource
{
    private const int SnapshotHeaderSize = 0x10;
    private const int SnapshotChannelSize = 0xC;
    private const int SnapshotStatusSize = 0x8;

    public List<SnapshotChannelData> Channels = [];
    public List<SnapshotStatusData> SnapshotStatuses = [];

    public override void WriteToStream(ResourceBinaryWriter writer, Endian endianness = Endian.Agnostic)
    {
        int snapshotCount = GetSnapshotCountForWrite();

        DataSize = (uint)(
            SnapshotHeaderSize +
            (Channels.Count * SnapshotChannelSize) +
            (SnapshotStatuses.Count * SnapshotStatusSize));

        base.WriteToStream(writer, endianness);

        long channelsOffset = writer.BaseStream.Position + SnapshotHeaderSize;
        long statusesOffset = channelsOffset + (Channels.Count * SnapshotChannelSize);

        writer.Write(snapshotCount);
        writer.Write(Channels.Count);
        writer.Write(1);            // maiPad[0] (mixer state?)
        writer.Write(0x12345678);   // maiPad[1]
        writer.WriteSection(channelsOffset, Channels, SnapshotChannelData.Write);
        writer.WriteSection(statusesOffset, SnapshotStatuses, SnapshotStatusData.Write);
    }

    public override void ParseFromStream(ResourceBinaryReader reader, Endian endianness = Endian.Agnostic)
    {
        base.ParseFromStream(reader, endianness);

        int snapshotCount = reader.ReadInt32();
        int channelCount = reader.ReadInt32();
        _ = reader.ReadUInt64();

        long channelsOffset = reader.BaseStream.Position;
        long statusesOffset = channelsOffset + (channelCount * SnapshotChannelSize);

        Channels = reader.ParseSection(channelsOffset, channelCount, SnapshotChannelData.Read);
        SnapshotStatuses = reader.ParseSection(statusesOffset, snapshotCount * channelCount, SnapshotStatusData.Read);
    }

    public SnapshotData() : base() { }

    public SnapshotData(string path, Endian endianness = Endian.Agnostic) : base(path, endianness) { }

    private int GetSnapshotCountForWrite()
    {
        if (Channels.Count == 0)
        {
            if (SnapshotStatuses.Count != 0)
            {
                throw new InvalidDataException("Snapshot statuses cannot be written without at least one channel.");
            }

            return 0;
        }

        if (SnapshotStatuses.Count % Channels.Count != 0)
        {
            throw new InvalidDataException(
                $"Snapshot status count ({SnapshotStatuses.Count}) must be divisible by channel count ({Channels.Count}).");
        }

        return SnapshotStatuses.Count / Channels.Count;
    }
}

public struct SnapshotChannelData
{
    public uint Flags;
    public uint ChannelID;

    public static SnapshotChannelData Read(ResourceBinaryReader reader)
    {
        SnapshotChannelData channelData = new()
        {
            Flags = reader.ReadUInt32(),
            ChannelID = reader.ReadUInt32()
        };

        uint terminator = reader.ReadUInt32();
        if (terminator != 0xFFFFFFFF)
        {
            Console.Error.WriteLine($"Expected 0xFFFFFFFF at {reader.BaseStream.Position}, got {terminator}!");
        }

        return channelData;
    }

    public static void Write(ResourceBinaryWriter writer, SnapshotChannelData channelData)
    {
        writer.Write(channelData.Flags);
        writer.Write(channelData.ChannelID);
        writer.Write(0xFFFFFFFFu);
    }
}

public struct SnapshotStatusData
{
    public float TimeRemaining;
    public uint Flags;

    public static SnapshotStatusData Read(ResourceBinaryReader reader)
    {
        return new SnapshotStatusData
        {
            Flags = reader.ReadUInt32(),
            TimeRemaining = reader.ReadSingle()
        };
    }

    public static void Write(ResourceBinaryWriter writer, SnapshotStatusData statusData)
    {
        writer.Write(statusData.Flags);
        writer.Write(statusData.TimeRemaining);
    }
}
