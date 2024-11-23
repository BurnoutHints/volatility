namespace Volatility.Resources.InstanceList;

public class InstanceListBase : Resource
{
    public override ResourceType GetResourceType() => ResourceType.InstanceList;

    List<Instance> Instances = [];
    //uint ArraySize, NumInstances, VersionNumber;

    public InstanceListBase() : base() { }

    public InstanceListBase(string path) : base(path) { }

    public override void ParseFromStream(EndianAwareBinaryReader reader)
    {
        base.ParseFromStream(reader);

        IntPtr instanceList = reader.ReadInt32();
        uint size = reader.ReadUInt32();
        uint numInstances = reader.ReadUInt32();

        // Version
        if (reader.ReadUInt32() != 1)
        {
            throw new Exception("Version mismatch!");
        }

        reader.BaseStream.Seek(instanceList, SeekOrigin.Begin);

        for (int i = 0; i < numInstances; i++) 
        {
            ModelPtr _model = (nint)reader.ReadUInt32();
            short _backdropZoneID = reader.ReadInt16();

            // reader.ReadUInt16(); reader.ReadUInt32();
            reader.BaseStream.Seek(0x6, SeekOrigin.Current);

            float _maxVisibleDistanceSquared = reader.ReadSingle();
            Transform _transform = Transform.ReadMatrix44AffineAsTransform(reader);

            Instances[i] = new Instance
            {
                Model = (nint)reader.ReadUInt32(),
                BackdropZoneID = _backdropZoneID,
                MaxVisibleDistanceSquared = _maxVisibleDistanceSquared,
                Transform = _transform
            };
        }
    }
}

public struct Instance
{
    public ModelPtr Model;
    public short BackdropZoneID;
    //ushort Padding1; uint Padding2;
    public float MaxVisibleDistanceSquared; // Unused?
    public Transform Transform;
}