using System.Numerics;

namespace Volatility.Resources;

public class SnapshotData : BinaryResource
{
    public override ResourceType GetResourceType() => ResourceType.SnapshotData;
    public override Platform GetResourcePlatform() => Platform.Agnostic;

    public List<SnapshotChannelData> Channels = [];
    public List<SnapshotStatusData> SnapshotStatuses = [];

    public override void WriteToStream(EndianAwareBinaryWriter writer, Endian endianness = Endian.Agnostic)
    {
        DataSize = (uint)(0x10 + (Channels.Count * 0xC) + (SnapshotStatuses.Count * 0x8));

        base.WriteToStream(writer, endianness);

        writer.Write(SnapshotStatuses.Count);
        writer.Write(Channels.Count);

        foreach (SnapshotChannelData channelData in Channels)
        {
            writer.Write(channelData.Flags);
            writer.Write(channelData.ChannelID);
            writer.Write(0xFFFFFFFF);
        }

        foreach (SnapshotStatusData statusData in SnapshotStatuses)
        {
            writer.Write(statusData.Flags);
            writer.Write(statusData.TimeRemaining);
        }
    }
    public override void ParseFromStream(ResourceBinaryReader reader, Endian endianness = Endian.Agnostic)
    {
        base.ParseFromStream(reader, endianness);

        int numSnapshots = reader.ReadInt32(); // Snapshot data
        int numChannels = reader.ReadInt32();  // Channel data

        _ = reader.ReadUInt64();

        // Channel data
        for (int i = 0; i < numSnapshots; i++) 
        {
            Channels.Add(new SnapshotChannelData()
            {
                Flags = reader.ReadUInt32(),
                ChannelID = reader.ReadUInt32()
            });
            uint invalid = reader.ReadUInt32();
            if (invalid != 0xFFFFFFFF)
            {
                Console.Error.WriteLine($"Expected 0xFFFFFFFF at {reader.BaseStream.Position}, got {invalid}!");
            }
        }

        // Snapshot data
        for (int i = 0; i < (numSnapshots * numChannels); i++)
        {
            SnapshotStatuses.Add(new SnapshotStatusData() 
            { 
                Flags = reader.ReadUInt32(), 
                TimeRemaining = reader.ReadSingle() 
            });
        }
    }

    public SnapshotData() : base() { }

    public SnapshotData(string path, Endian endianness = Endian.Agnostic) : base(path, endianness) { }
}

public struct SnapshotChannelData
{
    public uint Flags;
    public uint ChannelID;
} 

public struct SnapshotStatusData
{
    public float TimeRemaining;
    public uint Flags;
}