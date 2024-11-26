using static Volatility.Utilities.MatrixUtilities;

namespace Volatility.Resources.InstanceList;

public class InstanceListBase : Resource
{
    public override ResourceType GetResourceType() => ResourceType.InstanceList;
    
    [EditorLabel("Number of instances"), EditorCategory("Instance List"), EditorTooltip("The amount of instances that have a model assigned, but NOT the size of the entire instance array.")]
    public uint NumInstances;

    [EditorLabel("Instances"), EditorCategory("Instance List"), EditorTooltip("The list of instances in this list.")]
    public List<Instance> Instances = [];

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
            reader.BaseStream.Seek(instanceListPtr.ToInt32() + 0x50 * i, SeekOrigin.Begin);

            ModelPtr _model = (ModelPtr)reader.ReadUInt32();
            short _backdropZoneID = reader.ReadInt16();

            //ushort _padding1 = reader.ReadUInt16(); 
            //uint _padding2 = reader.ReadUInt32();
            reader.BaseStream.Seek(0x6, SeekOrigin.Current);

            float _maxVisibleDistanceSquared = reader.ReadSingle();

            Transform _transform = Matrix44AffineToTransform(ReadMatrix4x4(reader));

            reader.BaseStream.Seek(instanceListPtr.ToInt32() + 0x50 * (int)size + 0x10 * i, SeekOrigin.Begin);

            Instances.Add(new Instance
            {
                Model = _model,
                BackdropZoneID = _backdropZoneID,
                // Padding1 = _padding1, Padding2 = _padding2,
                MaxVisibleDistanceSquared = _maxVisibleDistanceSquared,
                Transform = _transform,
                ResourceId = new ResourceID
                {
                    ID = reader.ReadBytes(4),
                    Endian = reader.GetEndianness()
                },
            });
        }
    }
}

public struct Instance
{
    [EditorLabel("Resource ID"), EditorCategory("InstanceList/Instances"), EditorTooltip("The reference to the resource placed by this instance.")]
    public ResourceID ResourceId;

    [EditorLabel("Transform"), EditorCategory("InstanceList/Instances"), EditorTooltip("The location, rotation, and scale of this instance.")]
    public Transform Transform;

    [EditorLabel("Transform"), EditorCategory("InstanceList/Instances"), EditorTooltip("If this is a backdrop, the PVS Zone ID that this backdrop represents.")]
    public short BackdropZoneID;
    
    // public ushort Padding1; public uint Padding2;

    [EditorLabel("Max Visible Distance Squared"), EditorCategory("InstanceList/Instances"), EditorTooltip("The maximum distance that this instance can be seen (in meters), squared.")]
    public float MaxVisibleDistanceSquared; // Unused?

    [EditorHidden]
    public ModelPtr Model;  // Always seems to be zero. May be a runtime variable? Hiding for now.
}