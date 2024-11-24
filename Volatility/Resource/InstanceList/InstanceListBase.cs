using static Volatility.Utilities.MatrixUtilities;

namespace Volatility.Resources.InstanceList;

public class InstanceListBase : Resource
{
    public override ResourceType GetResourceType() => ResourceType.InstanceList;

    List<Instance> Instances = [];

    [EditorLabel("Number of instances"), EditorCategory("Instance List"), EditorTooltip("The amount of instances that have a model assigned, but NOT the size of the entire instance array.")]
    uint NumInstances;

    //uint ArraySize, VersionNumber;

    public InstanceListBase() : base() { }

    public InstanceListBase(string path) : base(path) { }

    public override void ParseFromStream(EndianAwareBinaryReader reader)
    {
        base.ParseFromStream(reader);

        IntPtr instanceListPtr = reader.ReadInt32();
        uint size = reader.ReadUInt32();
        NumInstances = reader.ReadUInt32();

        // Version
        if (reader.ReadUInt32() != 1)
        {
            throw new Exception("Version mismatch!");
        }

        reader.BaseStream.Seek(instanceListPtr, SeekOrigin.Begin);

        for (int i = 0; i < size; i++) 
        {
            long index = reader.BaseStream.Position;
            
            ModelPtr _model = (nint)reader.ReadUInt32();
            short _backdropZoneID = reader.ReadInt16();

            //ushort _padding1 = reader.ReadUInt16(); 
            //uint _padding2 = reader.ReadUInt32();
            reader.BaseStream.Seek(0x6, SeekOrigin.Current);

            float _maxVisibleDistanceSquared = reader.ReadSingle();
            Transform _transform = Matrix44AffineToTransform(ReadMatrix4x4(reader));

            Instances.Add(new Instance
            {
                Model = (nint)reader.ReadUInt32(),
                BackdropZoneID = _backdropZoneID,
                // Padding1 = _padding1, Padding2 = _padding2,
                MaxVisibleDistanceSquared = _maxVisibleDistanceSquared,
                Transform = _transform
            });

            reader.BaseStream.Seek(index, SeekOrigin.Begin);
        }
    }
}

public struct Instance
{
    public ModelPtr Model;
    public short BackdropZoneID;
    // public ushort Padding1; public uint Padding2;
    public float MaxVisibleDistanceSquared; // Unused?
    public Transform Transform;
}