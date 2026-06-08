using Volatility.Utilities;

using static Volatility.Utilities.MatrixUtilities;

namespace Volatility.Resources;

// The Instance List resource type contains lists of models along with their
// respective locations in the game world. It serves as one of the top-level
// resource types for track unit loading.
//
// Learn More:
// https://burnout.wiki/wiki/Instance_List

[ResourceDefinition(ResourceType.InstanceList)]
[ResourceRegistration(RegistrationPlatforms.All, EndianMapped = true)]
public class InstanceList : Resource
{
    private const int SectionAlignment = 0x10;

    [EditorLabel("Number of instances"), EditorCategory("Instance List"), EditorReadOnly, EditorTooltip("The amount of instances that have a model assigned, but NOT the size of the entire instance array.")]
    public uint NumInstances;

    [EditorLabel("Instances"), EditorCategory("Instance List"), EditorTooltip("The list of instances in this list.")]
    public List<Instance> Instances = [];

    public InstanceList() : base() { }

    public InstanceList(string path, Endian endianness = Endian.Agnostic) : base(path, endianness) { }

    public override void WriteToStream(ResourceBinaryWriter writer, Endian endianness = Endian.Agnostic)
    {
        base.WriteToStream(writer, endianness);

        int instanceBlockSize = GetInstanceBlockSize(ResourceArch);
        uint entryCount = (uint)Instances.Count;
        if (NumInstances > entryCount)
        {
            throw new InvalidDataException(
                $"NumInstances ({NumInstances}) cannot exceed the total array size ({entryCount}).");
        }

        long currentOffset = GetHeaderSize(ResourceArch);
        long instanceListOffset = ResourceUtilities.GetSectionOffset(
            ref currentOffset,
            checked((int)(entryCount * instanceBlockSize)),
            SectionAlignment);

        writer.WritePointer((ulong)instanceListOffset, ResourceArch);
        writer.Write(entryCount);
        writer.Write(NumInstances);
        writer.Write(1u);

        writer.WriteSection(instanceListOffset, Instances, (w, instance) => WriteInstanceBlock(w, instance, ResourceArch));
    }

    public override void ParseFromStream(ResourceBinaryReader reader, Endian endianness = Endian.Agnostic)
    {
        base.ParseFromStream(reader, endianness);

        ulong instanceListPtr = reader.ReadPointer(ResourceArch);
        uint entries = reader.ReadUInt32();
        NumInstances = reader.ReadUInt32();

        uint version = reader.ReadUInt32();
        if (version != 1)
        {
            throw new InvalidDataException($"Version mismatch! Version should be 1. (Found version {version})");
        }

        Instances.Clear();

        long instanceBlockSize = GetInstanceBlockSize(ResourceArch);
        if (entries > 0 && instanceListPtr == 0)
        {
            throw new InvalidDataException("Instance list pointer is null, but the resource declares instance entries.");
        }

        if (NumInstances > entries)
        {
            throw new InvalidDataException(
                $"Invalid InstanceList header: NumInstances ({NumInstances}) cannot exceed array size ({entries}).");
        }

        if (instanceListPtr != 0 && instanceListPtr < (ulong)GetHeaderSize(ResourceArch))
        {
            throw new InvalidDataException(
                $"Invalid InstanceList pointer 0x{instanceListPtr:X}. Instance data overlaps the resource header.");
        }

        long instanceDataLength = checked(instanceBlockSize * (long)entries);
        if (instanceListPtr != 0 && ((long)instanceListPtr + instanceDataLength) > reader.BaseStream.Length)
        {
            throw new InvalidDataException(
                $"Instance data range 0x{instanceListPtr:X}-0x{((long)instanceListPtr + instanceDataLength):X} exceeds stream length 0x{reader.BaseStream.Length:X}.");
        }

        long importBlockOffset = (long)instanceListPtr + instanceDataLength;

        for (int i = 0; i < entries; i++)
        {
            long instanceOffset = (long)instanceListPtr + (instanceBlockSize * i);

            reader.ParseSection(instanceOffset, r => ReadInstance(r, ResourceArch, importBlockOffset), out Instance instance);
            Instances.Add(instance);
        }
    }

    private static int GetInstanceBlockSize(Arch arch)
    {
        return arch == Arch.x64 ? 0x60 : 0x50;
    }

    private static int GetHeaderSize(Arch arch)
    {
        return ResourceUtilities.GetPointerSize(arch) + (sizeof(uint) * 3);
    }

    private static Instance ReadInstance(
        ResourceBinaryReader reader,
        Arch arch,
        long importBlockOffset)
    {
        long blockStart = reader.BaseStream.Position;

        ResourceImport.ReadExternalImport(blockStart, reader, importBlockOffset, out ResourceImport modelReference);
        reader.BaseStream.Seek(blockStart + ResourceUtilities.GetPointerSize(arch), SeekOrigin.Begin);

        short backdropZoneId = reader.ReadInt16();
        reader.BaseStream.Seek(0x6, SeekOrigin.Current);
        float maxVisibleDistanceSquared = reader.ReadSingle();
        if (arch == Arch.x64)
        {
            reader.BaseStream.Seek(0xC, SeekOrigin.Current);
        }

        Matrix44Affine transformMatrix = ReadMatrix44Affine(reader);
        Transform transform = Matrix44AffineToTransform(transformMatrix);

        return new Instance
        {
            ModelReference = modelReference,
            BackdropZoneID = backdropZoneId,
            MaxVisibleDistanceSquared = maxVisibleDistanceSquared,
            Transform = transform,
            TransformMatrix = transformMatrix,
        };
    }

    private static void WriteInstanceBlock(ResourceBinaryWriter writer, Instance instance, Arch arch)
    {
        long blockStart = writer.BaseStream.Position;

        writer.WritePointer(0, arch);
        writer.Write(instance.BackdropZoneID);
        writer.WriteFixedBytes(null, 0x6);
        writer.Write(instance.MaxVisibleDistanceSquared);
        if (arch == Arch.x64)
        {
            writer.WriteFixedBytes(null, 0xC);
        }

        Matrix44Affine transformMatrix = instance.TransformMatrix != default
            ? instance.TransformMatrix
            : TransformToMatrix44Affine(instance.Transform);
        WriteMatrix44Affine(writer, transformMatrix);

        int remaining = GetInstanceBlockSize(arch) - (int)(writer.BaseStream.Position - blockStart);
        if (remaining < 0)
        {
            throw new InvalidDataException(
                $"Instance block overflow. Wrote 0x{writer.BaseStream.Position - blockStart:X} bytes into a 0x{GetInstanceBlockSize(arch):X} byte block.");
        }

        if (remaining > 0)
        {
            writer.Write(new byte[remaining]);
        }
    }

    public override IEnumerable<KeyValuePair<long, ResourceImport>> GetExternalImports()
    {
        int instanceBlockSize = GetInstanceBlockSize(ResourceArch);
        long instanceListOffset = ResourceUtilities.AlignOffset(GetHeaderSize(ResourceArch), SectionAlignment);
        for (int i = 0; i < Instances.Count; i++)
        {
            ResourceImport modelReference = Instances[i].ModelReference;
            if (!modelReference.ExternalImport)
            {
                continue;
            }

            yield return new KeyValuePair<long, ResourceImport>(
                instanceListOffset + (i * instanceBlockSize),
                modelReference);
        }
    }
}

public struct Instance
{
    [EditorLabel("Model Reference"), EditorCategory("InstanceList/Instances"), EditorTooltip("The external model import referenced by this instance.")]
    public ResourceImport ModelReference;

    [EditorLabel("Transform"), EditorCategory("InstanceList/Instances"), EditorTooltip("The location, rotation, and scale of this instance.")]
    public Transform Transform;

    [EditorHidden]
    public Matrix44Affine TransformMatrix;

    [EditorLabel("Transform"), EditorCategory("InstanceList/Instances"), EditorTooltip("If this is a backdrop, the PVS Zone ID that this backdrop represents.")]
    public short BackdropZoneID;

    [EditorHidden]
    public float MaxVisibleDistanceSquared;
}
