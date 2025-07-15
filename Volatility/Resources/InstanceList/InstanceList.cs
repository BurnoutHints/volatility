using Volatility.Extensions;

using static Volatility.Utilities.MatrixUtilities;

namespace Volatility.Resources;

// The Instance List resource type contains lists of models along with their 
// respective locations in the game world. It serves as one of the top-level 
// resource types for track unit loading.

// Learn More:
// https://burnout.wiki/wiki/Instance_List

public class InstanceList : Resource
{
    public override ResourceType GetResourceType() => ResourceType.InstanceList;
    
    [EditorLabel("Number of instances"), EditorCategory("Instance List"), EditorReadOnly, EditorTooltip("The amount of instances that have a model assigned, but NOT the size of the entire instance array.")]
    public uint NumInstances;

    [EditorLabel("Instances"), EditorCategory("Instance List"), EditorTooltip("The list of instances in this list.")]
    public List<Instance> Instances = [];

    //uint ArraySize, VersionNumber;

    public InstanceList() : base() { }

    public InstanceList(string path, Endian endianness = Endian.Agnostic) : base(path, endianness) { }

    public override void ParseFromStream(BinaryReader reader, Endian n = Endian.Agnostic)
    {
        base.ParseFromStream(reader, n);

        // Absolute pointers (not relative to any specific point in the file)
        long instanceListPtr = (long)reader.ReadPointer(GetResourceArch(), n);

        uint entries = reader.ReadUInt32(n);
        NumInstances = reader.ReadUInt32(n);

        // Version
        if (reader.ReadUInt32() != 1)
        {
            throw new Exception("Version mismatch!");
        }

        reader.BaseStream.Seek(instanceListPtr, SeekOrigin.Begin);

        long instanceBlockSize = GetResourceArch() == Arch.x64 ? 0x60 : 0x50;

        for (int i = 0; i < entries; i++) 
        {
            reader.BaseStream.Seek(instanceListPtr + (instanceBlockSize * i), SeekOrigin.Begin);

            ResourceImport.ReadExternalImport(fileOffset: reader.BaseStream.Position, reader, n, instanceListPtr + (instanceBlockSize * entries), out ResourceImport _model);

            if (GetResourceArch() == Arch.x64) _ = reader.ReadUInt32(n);

            short _backdropZoneID = reader.ReadInt16(n);

            reader.BaseStream.Seek(0x6, SeekOrigin.Current);

            float _maxVisibleDistanceSquared = reader.ReadSingle(n);

            Transform _transform = Matrix44AffineToTransform(ReadMatrix44Affine(reader, n));

            reader.BaseStream.Seek(instanceListPtr + instanceBlockSize * entries + 0x10 * i, SeekOrigin.Begin);

            Instances.Add(new Instance
            {
                ModelReference = _model,
                BackdropZoneID = _backdropZoneID,
                MaxVisibleDistanceSquared = _maxVisibleDistanceSquared,
                Transform = _transform,
                ResourceId = new ResourceImport
                {
                    ReferenceID = reader.ReadUInt32(n),
                    ExternalImport = false
                },
            });
        }
    }
}

public struct Instance
{
    [EditorLabel("Resource ID"), EditorCategory("InstanceList/Instances"), EditorTooltip("The reference to the resource placed by this instance.")]
    public ResourceImport ResourceId;

    [EditorLabel("Transform"), EditorCategory("InstanceList/Instances"), EditorTooltip("The location, rotation, and scale of this instance.")]
    public Transform Transform;

    [EditorLabel("Transform"), EditorCategory("InstanceList/Instances"), EditorTooltip("If this is a backdrop, the PVS Zone ID that this backdrop represents.")]
    public short BackdropZoneID;
    
    // public ushort Padding1; public uint Padding2;

    [EditorLabel("Max Visible Distance Squared"), EditorCategory("InstanceList/Instances"), EditorTooltip("The maximum distance that this instance can be seen (in meters), squared.")]
    public float MaxVisibleDistanceSquared; // Unused?
    
    public ResourceImport ModelReference;
}