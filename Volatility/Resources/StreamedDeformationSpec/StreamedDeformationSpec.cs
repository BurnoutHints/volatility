using System.Numerics;
using System.Reflection.PortableExecutable;
using Volatility.Utilities;

namespace Volatility.Resources;

// The StreamedDeformationSpec resource type defines per-vehicle
// deformation data such as tag points, IK body parts, wheels, sensors,
// locator tags, and glass panes.
//
// Learn More:
// https://burnout.wiki/wiki/Streamed_Deformation

public struct WheelSpec
{
    public const int Size = 0x30;

    public Vector3 Position;
    public Vector3 Scale;
    public int TagPointIndex;

    public WheelSpec(ResourceBinaryReader reader)
    {
        Position = reader.ReadVector3();
        Scale = reader.ReadVector3();
        TagPointIndex = reader.ReadInt32();
        reader.BaseStream.Seek(0xC, SeekOrigin.Current);
    }
}

public struct SensorSpec
{
    public const int Size = 0x40;

    public Vector3 InitialOffset;
    public float[] DirectionParams;
    public float Radius;
    public byte[] NextSensor;
    public byte SceneIndex;
    public byte AbsorbtionLevel;
    public byte[] NextBoundarySensor;

    public SensorSpec(ResourceBinaryReader reader)
    {
        InitialOffset = reader.ReadVector3();

        DirectionParams = new float[6];
        for (int i = 0; i < DirectionParams.Length; i++)
        {
            DirectionParams[i] = reader.ReadSingle();
        }

        Radius = reader.ReadSingle();
        NextSensor = reader.ReadBytes(6);
        SceneIndex = reader.ReadByte();
        AbsorbtionLevel = reader.ReadByte();
        NextBoundarySensor = reader.ReadBytes(2);
        reader.BaseStream.Seek(0xA, SeekOrigin.Current);
    }
}

public struct TagPointSpec
{
    public const int Size = 0x50;

    public Vector3Plus OffsetFromAAndWeightA;
    public Vector3Plus OffsetFromBAndWeightB;
    public Vector3Plus InitialPositionAndDetachThreshold;
    public float WeightA;
    public float WeightB;
    public float DetachThresholdSquared;
    public short DeformationSensorA;
    public short DeformationSensorB;
    public sbyte JointIndex;
    public bool SkinnedPoint;

    public TagPointSpec(ResourceBinaryReader reader)
    {
        OffsetFromAAndWeightA = reader.ReadVector3Plus();
        OffsetFromBAndWeightB = reader.ReadVector3Plus();
        InitialPositionAndDetachThreshold = reader.ReadVector3Plus();
        WeightA = reader.ReadSingle();
        WeightB = reader.ReadSingle();
        DetachThresholdSquared = reader.ReadSingle();
        DeformationSensorA = reader.ReadInt16();
        DeformationSensorB = reader.ReadInt16();
        JointIndex = reader.ReadSByte();
        SkinnedPoint = reader.ReadBoolean();
        reader.BaseStream.Seek(0xE, SeekOrigin.Current);
    }
}

public struct IKDrivenPointSpec
{
    public const int Size = 0x20;

    public Vector3 InitialPos;
    public float DistanceFromA;
    public float DistanceFromB;
    public short TagPointIndexA;
    public short TagPointIndexB;

    public IKDrivenPointSpec(ResourceBinaryReader reader)
    {
        InitialPos = reader.ReadVector3();
        DistanceFromA = reader.ReadSingle();
        DistanceFromB = reader.ReadSingle();
        TagPointIndexA = reader.ReadInt16();
        TagPointIndexB = reader.ReadInt16();
        reader.BaseStream.Seek(0x4, SeekOrigin.Current);
    }
}

public struct LocatorPointSpec
{
    public const int Size = 0x50;

    public Matrix4x4 LocatorMatrix;
    public int TagPointType;
    public short IkPartIndex;
    public byte SkinPoint;

    public LocatorPointSpec(ResourceBinaryReader reader)
    {
        LocatorMatrix = MatrixUtilities.ReadMatrix44(reader);
        TagPointType = reader.ReadInt32();
        IkPartIndex = reader.ReadInt16();
        SkinPoint = reader.ReadByte();
        reader.BaseStream.Seek(0x9, SeekOrigin.Current);
    }
}

public struct LocatorPointSpecList
{
    public uint Count;
    public ulong Offset;

    public LocatorPointSpecList(ResourceBinaryReader reader, Arch arch)
    {
        Count = reader.ReadUInt32();
        if (arch == Arch.x64)
        {
            reader.BaseStream.Seek(0x4, SeekOrigin.Current);
            Offset = reader.ReadUInt64();
        }
        else
        {
            Offset = reader.ReadUInt32();
        }
    }
}

public struct BBoxPointSkinData
{
    public const int Size = 0x20;

    public Vector3 Vertex;
    public float[] Weights;
    public byte[] BoneIndices;

    public BBoxPointSkinData(ResourceBinaryReader reader)
    {
        Vertex = reader.ReadVector3();
        Weights =
        [
            reader.ReadSingle(),
            reader.ReadSingle(),
            reader.ReadSingle()
        ];
        BoneIndices = reader.ReadBytes(3);
        reader.BaseStream.Seek(0x1, SeekOrigin.Current);
    }
}

public struct BodyPartBBoxSpec
{
    public const int Size = 0x180;

    public Matrix4x4 Orientation;
    public List<BBoxPointSkinData> CornerSkinData;
    public BBoxPointSkinData CentreSkinData;
    public BBoxPointSkinData JointSkinData;

    public BodyPartBBoxSpec(ResourceBinaryReader reader)
    {
        Orientation = MatrixUtilities.ReadMatrix44(reader);

        CornerSkinData = new List<BBoxPointSkinData>(8);
        for (int i = 0; i < 8; i++)
        {
            CornerSkinData.Add(new BBoxPointSkinData(reader));
        }

        CentreSkinData = new BBoxPointSkinData(reader);
        JointSkinData = new BBoxPointSkinData(reader);
    }
}

public struct DeformationJointSpec
{
    public const int Size = 0x40;

    public Vector3 JointPosition;
    public Vector3 JointAxis;
    public Vector3 JointDefaultDirection;
    public float MaxJointAngle;
    public float JointDetachThreshold;
    public int JointType;

    public DeformationJointSpec(ResourceBinaryReader reader)
    {
        JointPosition = reader.ReadVector3();
        JointAxis = reader.ReadVector3();
        JointDefaultDirection = reader.ReadVector3();
        MaxJointAngle = reader.ReadSingle();
        JointDetachThreshold = reader.ReadSingle();
        JointType = reader.ReadInt32();
        reader.BaseStream.Seek(0x4, SeekOrigin.Current);
    }
}

public struct IKBodyPartSpec
{
    public static int GetSize(Arch arch) => arch == Arch.x64 ? 0x1E4 : 0x1E0;

    public Matrix4x4 GraphicsTransform;
    public BodyPartBBoxSpec BBoxSkinData;
    public ulong JointSpecsOffset;
    public int NumJoints;
    public int PartGraphics;
    public int StartIndexOfDrivenPoints;
    public int NumberOfDrivenPoints;
    public int StartIndexOfTagPoints;
    public int NumberOfTagPoints;
    public int PartType;
    public List<DeformationJointSpec> JointSpecs;

    public IKBodyPartSpec(ResourceBinaryReader reader, Arch arch)
    {
        long structStart = reader.BaseStream.Position;

        GraphicsTransform = MatrixUtilities.ReadMatrix44(reader);
        BBoxSkinData = new BodyPartBBoxSpec(reader);
        JointSpecsOffset = reader.ReadPointer(arch);
        NumJoints = reader.ReadInt32();
        PartGraphics = reader.ReadInt32();
        StartIndexOfDrivenPoints = reader.ReadInt32();
        NumberOfDrivenPoints = reader.ReadInt32();
        StartIndexOfTagPoints = reader.ReadInt32();
        NumberOfTagPoints = reader.ReadInt32();
        PartType = reader.ReadInt32();

        JointSpecs = new List<DeformationJointSpec>(Math.Max(NumJoints, 0));

        long structEnd = structStart + GetSize(arch);
        long originalPosition = reader.BaseStream.Position;

        if (NumJoints > 0 && JointSpecsOffset != 0)
        {
            reader.BaseStream.Seek((long)JointSpecsOffset, SeekOrigin.Begin);
            for (int i = 0; i < NumJoints; i++)
            {
                JointSpecs.Add(new DeformationJointSpec(reader));
            }
        }

        reader.BaseStream.Seek(Math.Max(structEnd, originalPosition), SeekOrigin.Begin);
    }
}

public struct GlassPaneSpec
{
    public const int Size = 0x70;

    public Vector3 Normal;
    public Vector3[] CornerPositionOffsets;
    public short[] PointIndices;
    public bool[] SkinToControlPoint;
    public short ParentBodyPart;
    public short CrackSensor;
    public short SmashSensor;
    public int PartType;

    public GlassPaneSpec(ResourceBinaryReader reader)
    {
        Normal = reader.ReadVector3();

        CornerPositionOffsets = new Vector3[4];
        for (int i = 0; i < CornerPositionOffsets.Length; i++)
        {
            CornerPositionOffsets[i] = reader.ReadVector3();
        }

        PointIndices = new short[4];
        for (int i = 0; i < PointIndices.Length; i++)
        {
            PointIndices[i] = reader.ReadInt16();
        }

        SkinToControlPoint = new bool[4];
        for (int i = 0; i < SkinToControlPoint.Length; i++)
        {
            SkinToControlPoint[i] = reader.ReadByte() != 0;
        }

        ParentBodyPart = reader.ReadInt16();
        CrackSensor = reader.ReadInt16();
        SmashSensor = reader.ReadInt16();
        reader.BaseStream.Seek(0x2, SeekOrigin.Current);
        PartType = reader.ReadInt32();
        reader.BaseStream.Seek(0x8, SeekOrigin.Current);
    }
}

[ResourceDefinition(ResourceType.StreamedDeformationSpec)]
[ResourceRegistration(RegistrationPlatforms.All, EndianMapped = true)]
public class StreamedDeformationSpec : Resource
{
    public const int HeaderSize32 = 0x6B0;
    public const int HeaderSize64 = 0x6F0;
    private const int SectionAlignment = 0x10;

    public int VersionNumber { get; set; }
    public ulong TagPointDataOffset { get; set; }
    public int NumberOfTagPoints { get; set; }
    public ulong DrivenPointDataOffset { get; set; }
    public int NumberOfDrivenPoints { get; set; }
    public ulong IKPartDataOffset { get; set; }
    public int NumberOfIKParts { get; set; }
    public ulong GlassPaneDataOffset { get; set; }
    public int NumGlassPanes { get; set; }
    public LocatorPointSpecList GenericTagsInfo { get; set; }
    public LocatorPointSpecList CameraTagsInfo { get; set; }
    public LocatorPointSpecList LightTagsInfo { get; set; }
    public Vector3 HandlingBodyDimensions { get; set; }
    public List<WheelSpec> WheelSpecs { get; set; } = new();
    public List<SensorSpec> DeformationSensorSpecs { get; set; } = new();
    public Matrix4x4 CarModelSpaceToHandlingBodySpaceTransform { get; set; }
    public byte SpecID { get; set; }
    public byte NumVehicleBodies { get; set; }
    public byte NumDeformationSensors { get; set; }
    public byte NumGraphicsParts { get; set; }
    public Vector3 CurrentCOMOffset { get; set; }
    public Vector3 MeshOffset { get; set; }
    public Vector3 RigidBodyOffset { get; set; }
    public Vector3 CollisionOffset { get; set; }
    public Vector3 InertiaTensor { get; set; }
    public List<TagPointSpec> TagPointSpecs { get; set; } = new();
    public List<IKDrivenPointSpec> DrivenPoints { get; set; } = new();
    public List<LocatorPointSpec> GenericTags { get; set; } = new();
    public List<LocatorPointSpec> CameraTags { get; set; } = new();
    public List<LocatorPointSpec> LightTags { get; set; } = new();
    public List<IKBodyPartSpec> IKParts { get; set; } = new();
    public List<GlassPaneSpec> GlassPanes { get; set; } = new();

    public override void ParseFromStream(ResourceBinaryReader reader, Endian endianness = Endian.Agnostic)
    {
        base.ParseFromStream(reader, endianness);

        WheelSpecs.Clear();
        DeformationSensorSpecs.Clear();
        TagPointSpecs.Clear();
        DrivenPoints.Clear();
        GenericTags.Clear();
        CameraTags.Clear();
        LightTags.Clear();
        IKParts.Clear();
        GlassPanes.Clear();

        Arch arch = ResourceArch;

        VersionNumber = reader.ReadInt32();
        if (VersionNumber != 1)
        {
            throw new InvalidDataException($"Version mismatch! Version should be 1. (Found version {VersionNumber})");
        }

        if (arch == Arch.x64)
        {
            reader.BaseStream.Seek(0x4, SeekOrigin.Current);
        }

        TagPointDataOffset = reader.ReadPointer(arch);
        NumberOfTagPoints = reader.ReadArchDependInt(arch);
        DrivenPointDataOffset = reader.ReadPointer(arch);
        NumberOfDrivenPoints = reader.ReadArchDependInt(arch);
        IKPartDataOffset = reader.ReadPointer(arch);
        NumberOfIKParts = reader.ReadArchDependInt(arch);
        GlassPaneDataOffset = reader.ReadPointer(arch);
        NumGlassPanes = reader.ReadArchDependInt(arch);
        GenericTagsInfo = new(reader, arch);
        CameraTagsInfo = new (reader, arch);
        LightTagsInfo = new(reader, arch);

        reader.BaseStream.Seek(ResourceUtilities.GetPointerSize(ResourceArch), SeekOrigin.Current);

        HandlingBodyDimensions = reader.ReadVector3();

        for (int i = 0; i < 4; i++)
        {
            WheelSpecs.Add(new WheelSpec(reader));
        }

        for (int i = 0; i < 20; i++)
        {
            DeformationSensorSpecs.Add(new SensorSpec(reader));
        }

        CarModelSpaceToHandlingBodySpaceTransform = MatrixUtilities.ReadMatrix44(reader);

        SpecID = reader.ReadByte();
        NumVehicleBodies = reader.ReadByte();
        NumDeformationSensors = reader.ReadByte();
        NumGraphicsParts = reader.ReadByte();
        reader.BaseStream.Seek(0xC, SeekOrigin.Current);

        CurrentCOMOffset = reader.ReadVector3();
        MeshOffset = reader.ReadVector3();
        RigidBodyOffset = reader.ReadVector3();
        CollisionOffset = reader.ReadVector3();
        InertiaTensor = reader.ReadVector3();

        reader.ParseSection(TagPointDataOffset, NumberOfTagPoints, r => new TagPointSpec(r), TagPointSpecs);
        reader.ParseSection(DrivenPointDataOffset, NumberOfDrivenPoints, r => new IKDrivenPointSpec(r), DrivenPoints);
        reader.ParseSection(GenericTagsInfo.Offset, (int)GenericTagsInfo.Count, r => new LocatorPointSpec(r), GenericTags);
        reader.ParseSection(CameraTagsInfo.Offset, (int)CameraTagsInfo.Count, r => new LocatorPointSpec(r), CameraTags);
        reader.ParseSection(LightTagsInfo.Offset, (int)LightTagsInfo.Count, r => new LocatorPointSpec(r), LightTags);
        reader.ParseSection(GlassPaneDataOffset, NumGlassPanes, r => new GlassPaneSpec(r), GlassPanes);
        reader.ParseSection(IKPartDataOffset, NumberOfIKParts, r => new IKBodyPartSpec(r, arch), IKParts);
    }

    public override void WriteToStream(ResourceBinaryWriter writer, Endian endianness = Endian.Agnostic)
    {
        base.WriteToStream(writer, endianness);

        Arch arch = ResourceArch;
        int headerSize = arch == Arch.x64 ? HeaderSize64 : HeaderSize32;
        int ikPartStructSize = IKBodyPartSpec.GetSize(arch);

        if (VersionNumber == 0)
        {
            VersionNumber = 1;
        }

        if (VersionNumber != 1)
        {
            throw new InvalidDataException($"Version mismatch! Version should be 1. (Found version {VersionNumber})");
        }

        if (WheelSpecs.Count > 4)
        {
            throw new InvalidDataException($"StreamedDeformationSpec only supports 4 wheel specs. Found {WheelSpecs.Count}.");
        }

        if (DeformationSensorSpecs.Count > 20)
        {
            throw new InvalidDataException($"StreamedDeformationSpec only supports 20 sensor specs. Found {DeformationSensorSpecs.Count}.");
        }

        List<WheelSpec> wheelSpecs = ResourceUtilities.GetFixedSizeList(WheelSpecs, 4);
        List<SensorSpec> sensorSpecs = ResourceUtilities.GetFixedSizeList(DeformationSensorSpecs, 20);

        NumberOfTagPoints = TagPointSpecs.Count;
        NumberOfDrivenPoints = DrivenPoints.Count;
        NumberOfIKParts = IKParts.Count;
        NumGlassPanes = GlassPanes.Count;

        long currentOffset = headerSize;

        TagPointDataOffset = ResourceUtilities.GetSectionOffset(ref currentOffset, NumberOfTagPoints, TagPointSpec.Size, SectionAlignment);
        DrivenPointDataOffset = ResourceUtilities.GetSectionOffset(ref currentOffset, NumberOfDrivenPoints, IKDrivenPointSpec.Size, SectionAlignment);
        IKPartDataOffset = ResourceUtilities.GetSectionOffset(ref currentOffset, NumberOfIKParts, ikPartStructSize, SectionAlignment);

        ulong[] jointSpecOffsets = new ulong[IKParts.Count];
        for (int i = 0; i < IKParts.Count; i++)
        {
            int jointCount = IKParts[i].JointSpecs?.Count ?? 0;
            if (jointCount > 0)
            {
                currentOffset = ResourceUtilities.AlignOffset(currentOffset, SectionAlignment);
                jointSpecOffsets[i] = (ulong)currentOffset;
                currentOffset += jointCount * DeformationJointSpec.Size;
            }
        }

        currentOffset = ResourceUtilities.AlignOffset(currentOffset, SectionAlignment);

        GlassPaneDataOffset = ResourceUtilities.GetSectionOffset(ref currentOffset, NumGlassPanes, GlassPaneSpec.Size, SectionAlignment);

        GenericTagsInfo = new LocatorPointSpecList
        {
            Count = (uint)GenericTags.Count,
            Offset = ResourceUtilities.GetSectionOffset(ref currentOffset, GenericTags.Count, LocatorPointSpec.Size, SectionAlignment)
        };
        CameraTagsInfo = new LocatorPointSpecList
        {
            Count = (uint)CameraTags.Count,
            Offset = ResourceUtilities.GetSectionOffset(ref currentOffset, CameraTags.Count, LocatorPointSpec.Size, SectionAlignment)
        };
        LightTagsInfo = new LocatorPointSpecList
        {
            Count = (uint)LightTags.Count,
            Offset = ResourceUtilities.GetSectionOffset(ref currentOffset, LightTags.Count, LocatorPointSpec.Size, SectionAlignment)
        };

        writer.Write(VersionNumber);
        if (arch == Arch.x64)
        {
            writer.Write(new byte[0x4]);
        }

        writer.WritePointer(TagPointDataOffset, arch);
        writer.WriteArchDependInt(NumberOfTagPoints, arch);
        writer.WritePointer(DrivenPointDataOffset, arch);
        writer.WriteArchDependInt(NumberOfDrivenPoints, arch);
        writer.WritePointer(IKPartDataOffset, arch);
        writer.WriteArchDependInt(NumberOfIKParts, arch);
        writer.WritePointer(GlassPaneDataOffset, arch);
        writer.WriteArchDependInt(NumGlassPanes, arch);
        WriteLocatorPointSpecList(writer, GenericTagsInfo, arch);
        WriteLocatorPointSpecList(writer, CameraTagsInfo, arch);
        WriteLocatorPointSpecList(writer, LightTagsInfo, arch);

        writer.WritePointer(0, arch);
        writer.Write(HandlingBodyDimensions, intrinsic: true);

        for (int i = 0; i < wheelSpecs.Count; i++)
        {
            WriteWheelSpec(writer, wheelSpecs[i]);
        }

        for (int i = 0; i < sensorSpecs.Count; i++)
        {
            WriteSensorSpec(writer, sensorSpecs[i]);
        }

        MatrixUtilities.WriteMatrix44(writer, CarModelSpaceToHandlingBodySpaceTransform);

        writer.Write(SpecID);
        writer.Write(NumVehicleBodies);
        writer.Write(NumDeformationSensors);
        writer.Write(NumGraphicsParts);
        writer.Write(new byte[0xC]);

        writer.Write(CurrentCOMOffset, intrinsic: true);
        writer.Write(MeshOffset, intrinsic: true);
        writer.Write(RigidBodyOffset, intrinsic: true);
        writer.Write(CollisionOffset, intrinsic: true);
        writer.Write(InertiaTensor, intrinsic: true);

        if (writer.BaseStream.Position != headerSize)
        {
            throw new InvalidDataException($"Header size mismatch while writing StreamedDeformationSpec. Expected 0x{headerSize:X}, wrote 0x{writer.BaseStream.Position:X}.");
        }

        writer.WriteSection(TagPointDataOffset, TagPointSpecs, WriteTagPointSpec);
        writer.WriteSection(DrivenPointDataOffset, DrivenPoints, WriteIKDrivenPointSpec);
        writer.WriteSection(IKPartDataOffset, IKParts, (w, part, index) => WriteIKBodyPartSpec(w, part, arch, jointSpecOffsets[index]));

        for (int i = 0; i < IKParts.Count; i++)
        {
            if (jointSpecOffsets[i] == 0)
            {
                continue;
            }

            writer.BaseStream.Position = (long)jointSpecOffsets[i];
            List<DeformationJointSpec> joints = IKParts[i].JointSpecs ?? [];
            for (int j = 0; j < joints.Count; j++)
            {
                WriteDeformationJointSpec(writer, joints[j]);
            }
        }

        writer.WriteSection(GlassPaneDataOffset, GlassPanes, WriteGlassPaneSpec);
        writer.WriteSection(GenericTagsInfo.Offset, GenericTags, WriteLocatorPointSpec);
        writer.WriteSection(CameraTagsInfo.Offset, CameraTags, WriteLocatorPointSpec);
        writer.WriteSection(LightTagsInfo.Offset, LightTags, WriteLocatorPointSpec);
    }

    public StreamedDeformationSpec() : base() { }

    public StreamedDeformationSpec(string path, Endian endianness = Endian.Agnostic) : base(path, endianness) { }

    // Section writers

    private static void WriteLocatorPointSpecList(ResourceBinaryWriter writer, LocatorPointSpecList value, Arch arch)
    {
        writer.Write(value.Count);
        if (arch == Arch.x64)
        {
            writer.Write(0);
            writer.Write(value.Offset);
        }
        else
        {
            if (value.Offset > uint.MaxValue)
            {
                throw new InvalidDataException($"Locator pointer 0x{value.Offset:X} does not fit in a 32-bit StreamedDeformationSpec.");
            }

            writer.Write((uint)value.Offset);
        }
    }

    private static void WriteWheelSpec(ResourceBinaryWriter writer, WheelSpec value)
    {
        writer.Write(value.Position, intrinsic: true);
        writer.Write(value.Scale, intrinsic: true);
        writer.Write(value.TagPointIndex);
        writer.Write(new byte[0xC]);
    }

    private static void WriteSensorSpec(ResourceBinaryWriter writer, SensorSpec value)
    {
        writer.Write(value.InitialOffset, intrinsic: true);

        for (int i = 0; i < 6; i++)
        {
            writer.Write(i < value.DirectionParams?.Length ? value.DirectionParams[i] : 0f);
        }

        writer.Write(value.Radius);
        writer.WriteFixedBytes(value.NextSensor, 6);
        writer.Write(value.SceneIndex);
        writer.Write(value.AbsorbtionLevel);
        writer.WriteFixedBytes(value.NextBoundarySensor, 2);
        writer.Write(new byte[0xA]);
    }

    private static void WriteTagPointSpec(ResourceBinaryWriter writer, TagPointSpec value)
    {
        writer.Write(value.OffsetFromAAndWeightA);
        writer.Write(value.OffsetFromBAndWeightB);
        writer.Write(value.InitialPositionAndDetachThreshold);
        writer.Write(value.WeightA);
        writer.Write(value.WeightB);
        writer.Write(value.DetachThresholdSquared);
        writer.Write(value.DeformationSensorA);
        writer.Write(value.DeformationSensorB);
        writer.Write(value.JointIndex);
        writer.Write(value.SkinnedPoint);
        writer.Write(new byte[0xE]);
    }

    private static void WriteIKDrivenPointSpec(ResourceBinaryWriter writer, IKDrivenPointSpec value)
    {
        writer.Write(value.InitialPos, intrinsic: true);
        writer.Write(value.DistanceFromA);
        writer.Write(value.DistanceFromB);
        writer.Write(value.TagPointIndexA);
        writer.Write(value.TagPointIndexB);
        writer.Write(new byte[0x4]);
    }

    private static void WriteLocatorPointSpec(ResourceBinaryWriter writer, LocatorPointSpec value)
    {
        MatrixUtilities.WriteMatrix44(writer, value.LocatorMatrix);
        writer.Write(value.TagPointType);
        writer.Write(value.IkPartIndex);
        writer.Write(value.SkinPoint);
        writer.Write(new byte[0x9]);
    }

    private static void WriteIKBodyPartSpec(ResourceBinaryWriter writer, IKBodyPartSpec value, Arch arch, ulong jointSpecsOffset)
    {
        MatrixUtilities.WriteMatrix44(writer, value.GraphicsTransform);
        WriteBodyPartBBoxSpec(writer, value.BBoxSkinData);
        writer.WritePointer(jointSpecsOffset, arch);
        writer.Write(value.JointSpecs?.Count ?? 0);
        writer.Write(value.PartGraphics);
        writer.Write(value.StartIndexOfDrivenPoints);
        writer.Write(value.NumberOfDrivenPoints);
        writer.Write(value.StartIndexOfTagPoints);
        writer.Write(value.NumberOfTagPoints);
        writer.Write(value.PartType);
    }

    private static void WriteBodyPartBBoxSpec(ResourceBinaryWriter writer, BodyPartBBoxSpec value)
    {
        MatrixUtilities.WriteMatrix44(writer, value.Orientation);

        for (int i = 0; i < 8; i++)
        {
            WriteBBoxPointSkinData(
                writer,
                i < value.CornerSkinData?.Count ? value.CornerSkinData[i] : default
            );
        }

        WriteBBoxPointSkinData(writer, value.CentreSkinData);
        WriteBBoxPointSkinData(writer, value.JointSkinData);
    }

    private static void WriteBBoxPointSkinData(ResourceBinaryWriter writer, BBoxPointSkinData value)
    {
        writer.Write(value.Vertex, intrinsic: true);

        for (int i = 0; i < 3; i++)
        {
            writer.Write(i < value.Weights?.Length ? value.Weights[i] : 0f);
        }

        writer.WriteFixedBytes(value.BoneIndices, 3);
        writer.Write((byte)0);
    }

    private static void WriteDeformationJointSpec(ResourceBinaryWriter writer, DeformationJointSpec value)
    {
        writer.Write(value.JointPosition, intrinsic: true);
        writer.Write(value.JointAxis, intrinsic: true);
        writer.Write(value.JointDefaultDirection, intrinsic: true);
        writer.Write(value.MaxJointAngle);
        writer.Write(value.JointDetachThreshold);
        writer.Write(value.JointType);
        writer.Write(new byte[0x4]);
    }

    private static void WriteGlassPaneSpec(ResourceBinaryWriter writer, GlassPaneSpec value)
    {
        writer.Write(value.Normal, intrinsic: true);

        for (int i = 0; i < 4; i++)
        {
            writer.Write(i < value.CornerPositionOffsets?.Length ? value.CornerPositionOffsets[i] : default, intrinsic: true);
        }

        for (int i = 0; i < 4; i++)
        {
            writer.Write(i < value.PointIndices?.Length ? value.PointIndices[i] : (short)0);
        }

        for (int i = 0; i < 4; i++)
        {
            writer.Write((byte)((i < value.SkinToControlPoint?.Length && value.SkinToControlPoint[i]) ? 1 : 0));
        }

        writer.Write(value.ParentBodyPart);
        writer.Write(value.CrackSensor);
        writer.Write(value.SmashSensor);
        writer.Write(new byte[0x2]);
        writer.Write(value.PartType);
        writer.Write(new byte[0x8]);
    }
}
