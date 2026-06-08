using System.Numerics;
using System.Text;

namespace Volatility.Resources;

// The AttribSysVault resource type wraps VLT and BIN blobs which together define
// attribute collections used for vehicles, engines, surfaces, cameras, and more.
//
// Learn More:
// https://burnout.wiki/wiki/AttribSysVault

[ResourceDefinition(ResourceType.AttribSysVault)]
[ResourceRegistration(RegistrationPlatforms.All, EndianMapped = true)]
public class AttribSysVault : Resource
{
    public ulong VltDataOffset { get; set; }
    public uint VltSizeInBytes { get; set; }
    public ulong BinDataOffset { get; set; }
    public uint BinSizeInBytes { get; set; }

    public ulong VersionHash { get; set; }
    public ulong DepHash1 { get; set; }
    public ulong DepHash2 { get; set; }
    public int DepNop { get; set; }
    public List<string> Dependencies { get; set; } = [];
    public long StrUnknown1 { get; set; }
    public List<Dictionary<string, object>> Attributes { get; set; } = [];
    public List<string> Strings { get; set; } = [];
    public Dictionary<string, object> PtrN { get; set; } = [];
    public string Data { get; set; } = string.Empty;

    private const ulong EntryTypeAttribHash = 0xAD303B8F42B3307E;
    private static readonly Dictionary<ulong, string> ClassNames = new()
    {
        { 0x2E3B1DC7D248445E, "physicsvehiclebodyrollattribs" },
        { 0x52B81656F3ADF675, "burnoutcarasset" },
        { 0xF850281CA54C9B92, "physicsvehicleengineattribs" },
        { 0x3F9370FCF8D767AC, "physicsvehicledriftattribs" },
        { 0xDF956BC0568F138C, "physicsvehiclecollisionattribs" },
        { 0x4297B5841F5231CF, "physicsvehiclesuspensionattribs" },
        { 0x43462C59212A23CC, "physicsvehiclesteeringattribs" },
        { 0xE9EDA3B8C4EA3C84, "cameraexternalbehaviour" },
        { 0xF79C545E141DFFA6, "physicsvehiclebaseattribs" },
        { 0xF0FF4DFD660F5A54, "burnoutcargraphicsasset" },
        { 0xF3E3F8EF855F4F99, "camerabumperbehaviour" },
        { 0xEADE7049AF7AB31E, "physicsvehicleboostattribs" },
        { 0x966121397B502EED, "physicsvehiclehandling" },
        { 0x7F161D94482CB3BF, "vehicleengine" },
    };

    private static readonly Dictionary<ulong, Func<EndianAwareBinaryReader, Dictionary<string, object>>> PayloadReaders = new()
    {
        { 0xF850281CA54C9B92, ReadPhysicsVehicleEngineAttribs },
        { 0x3F9370FCF8D767AC, ReadPhysicsVehicleDriftAttribs },
        { 0xDF956BC0568F138C, ReadPhysicsVehicleCollisionAttribs },
        { 0x4297B5841F5231CF, ReadPhysicsVehicleSuspensionAttribs },
        { 0x43462C59212A23CC, ReadPhysicsVehicleSteeringAttribs },
        { 0x966121397B502EED, ReadPhysicsVehicleHandlingAttribs },
        { 0xEADE7049AF7AB31E, ReadPhysicsVehicleBoostAttribs },
        { 0xF3E3F8EF855F4F99, ReadCameraBumperBehaviourAttribs },
        { 0xE9EDA3B8C4EA3C84, ReadCameraExternalBehaviourAttribs },
        { 0xF0FF4DFD660F5A54, ReadBurnoutCarGraphicsAssetAttribs },
        { 0x52B81656F3ADF675, ReadBurnoutCarAssetAttribs },
        { 0x2E3B1DC7D248445E, ReadPhysicsVehicleBodyRollAttribs },
        { 0xF79C545E141DFFA6, ReadPhysicsVehicleBaseAttribs },
    };

    public override void ParseFromStream(ResourceBinaryReader reader, Endian endianness = Endian.Agnostic)
    {
        base.ParseFromStream(reader, endianness);

        Dependencies.Clear();
        Attributes.Clear();
        Strings.Clear();
        PtrN = [];
        Data = string.Empty;

        if (ResourceArch == Arch.x64)
        {
            VltDataOffset = reader.ReadUInt64();
            VltSizeInBytes = reader.ReadUInt32();
            reader.BaseStream.Seek(0x4, SeekOrigin.Current);
            BinDataOffset = reader.ReadUInt64();
            BinSizeInBytes = reader.ReadUInt32();
        }
        else
        {
            VltDataOffset = reader.ReadUInt32();
            VltSizeInBytes = reader.ReadUInt32();
            BinDataOffset = reader.ReadUInt32();
            BinSizeInBytes = reader.ReadUInt32();
        }

        long originalPosition = reader.BaseStream.Position;

        reader.BaseStream.Position = (long)VltDataOffset;
        byte[] vltBytes = reader.ReadBytes((int)VltSizeInBytes);

        reader.BaseStream.Position = (long)BinDataOffset;
        byte[] binBytes = reader.ReadBytes((int)BinSizeInBytes);

        reader.BaseStream.Position = originalPosition;

        List<PendingAttribute> pendingAttributes = [];
        using (EndianAwareBinaryReader vltReader = new(new MemoryStream(vltBytes, writable: false), reader.Endianness))
        {
            ParseVlt(vltReader, pendingAttributes);
        }

        using (EndianAwareBinaryReader binReader = new(new MemoryStream(binBytes, writable: false), reader.Endianness))
        {
            ParseBin(binReader, pendingAttributes);
        }
    }

    public override void WriteToStream(ResourceBinaryWriter writer, Endian endianness = Endian.Agnostic)
    {
        base.WriteToStream(writer, endianness);
        throw new NotImplementedException("Writing AttribSysVault is not implemented.");
    }

    public AttribSysVault() : base() { }

    public AttribSysVault(string path, Endian endianness = Endian.Agnostic)
        : base(path, endianness) { }

    private void ParseVlt(EndianAwareBinaryReader reader, List<PendingAttribute> pendingAttributes)
    {
        while (reader.BaseStream.Position < reader.BaseStream.Length)
        {
            ReadVltChunk(reader, pendingAttributes);
        }
    }

    private void ReadVltChunk(EndianAwareBinaryReader reader, List<PendingAttribute> pendingAttributes)
    {
        long chunkStart = reader.BaseStream.Position;
        string fourCc = ReadFourCc(reader);
        int size = reader.ReadInt32();
        if (size < 8)
        {
            throw new InvalidDataException($"Invalid AttribSys chunk size {size} for '{fourCc}'.");
        }

        switch (fourCc)
        {
            case "Vers":
                VersionHash = reader.ReadUInt64();
                break;
            case "DepN":
                ReadDepN(reader);
                break;
            case "StrN":
                StrUnknown1 = reader.ReadInt64();
                break;
            case "DatN":
                break;
            case "ExpN":
                ReadExpN(reader, pendingAttributes);
                break;
            case "PtrN":
                ReadPtrN(reader, size);
                break;
            default:
                throw new InvalidDataException($"Unknown AttribSys VLT chunk '{fourCc}'.");
        }

        reader.BaseStream.Position = chunkStart + size;
    }

    private void ReadDepN(EndianAwareBinaryReader reader)
    {
        long entryCount = reader.ReadInt64();
        DepHash1 = reader.ReadUInt64();
        DepHash2 = reader.ReadUInt64();
        DepNop = reader.ReadInt32();
        int entrySize = reader.ReadInt32();

        Dependencies = [];
        for (long i = 0; i < entryCount; i++)
        {
            Dependencies.Add(ReadFixedLengthString(reader, entrySize));
        }
    }

    private void ReadExpN(EndianAwareBinaryReader reader, List<PendingAttribute> pendingAttributes)
    {
        long nestedChunkCount = reader.ReadInt64();
        for (long i = 0; i < nestedChunkCount; i++)
        {
            AttribChunkInfo info = new()
            {
                Hash = reader.ReadUInt64(),
                EntryTypeHash = reader.ReadUInt64(),
                DataChunkSize = reader.ReadInt32(),
                DataChunkPosition = reader.ReadInt32(),
            };

            if (info.EntryTypeHash != EntryTypeAttribHash)
            {
                throw new InvalidDataException($"Unknown AttribSys entry type 0x{info.EntryTypeHash:X16}.");
            }

            long position = reader.BaseStream.Position;
            reader.BaseStream.Position = info.DataChunkPosition;
            AttribAttributeHeader header = ReadAttributeHeader(reader);
            pendingAttributes.Add(new PendingAttribute
            {
                Header = header,
                Info = info,
            });
            reader.BaseStream.Position = position;
        }
    }

    private void ReadPtrN(EndianAwareBinaryReader reader, int size)
    {
        int dataSize = size - 8;
        List<AttribPointerChunkData> allData = [];
        for (int i = 0; i < dataSize / 16; i++)
        {
            allData.Add(new AttribPointerChunkData
            {
                Ptr = reader.ReadUInt32(),
                Type = reader.ReadInt16(),
                Flag = reader.ReadInt16(),
                Data = reader.ReadUInt64(),
            });
        }

        PtrN = new Dictionary<string, object>
        {
            ["allData"] = allData
        };
    }

    private static AttribAttributeHeader ReadAttributeHeader(EndianAwareBinaryReader reader)
    {
        AttribAttributeHeader header = new()
        {
            CollectionHash = reader.ReadUInt64(),
            ClassHash = reader.ReadUInt64(),
            Unknown1 = Convert.ToBase64String(reader.ReadBytes(8)),
            ItemCount = reader.ReadInt32(),
            Unknown2 = reader.ReadInt32(),
            ItemCountDup = reader.ReadInt32(),
            ParameterCount = reader.ReadInt16(),
            ParametersToRead = reader.ReadInt16(),
            Unknown3 = Convert.ToBase64String(reader.ReadBytes(8)),
        };

        header.ClassName = ClassNames.TryGetValue(header.ClassHash, out string? className)
            ? className
            : $"unknown_{header.ClassHash:X16}".ToLowerInvariant();

        header.ParameterTypeHashes = new ulong[header.ParameterCount];
        for (int i = 0; i < header.ParameterCount; i++)
        {
            header.ParameterTypeHashes[i] = reader.ReadUInt64();
        }

        for (int i = 0; i < (header.ParametersToRead - header.ParameterCount); i++)
        {
            _ = reader.ReadUInt64();
        }

        header.Items = [];
        for (int i = 0; i < header.ItemCount; i++)
        {
            header.Items.Add(new AttribDataItem
            {
                Hash = reader.ReadUInt64(),
                Unknown1 = Convert.ToBase64String(reader.ReadBytes(4)),
                ParameterIdx = reader.ReadInt16(),
                Unknown2 = reader.ReadInt16(),
            });
        }

        return header;
    }

    private void ParseBin(EndianAwareBinaryReader reader, List<PendingAttribute> pendingAttributes)
    {
        long chunkStart = reader.BaseStream.Position;
        string fourCc = ReadFourCc(reader);
        int size = reader.ReadInt32();
        if (fourCc != "StrE")
        {
            throw new InvalidDataException($"Expected AttribSys BIN to start with 'StrE', found '{fourCc}'.");
        }

        long chunkEnd = chunkStart + size;
        Strings = [];
        while (reader.BaseStream.Position < chunkEnd)
        {
            Strings.Add(ReadCString(reader));
        }

        while (Strings.Count > 0 && string.IsNullOrEmpty(Strings[^1]))
        {
            Strings.RemoveAt(Strings.Count - 1);
        }

        reader.BaseStream.Position = chunkEnd;

        Attributes = [];
        foreach (PendingAttribute pendingAttribute in pendingAttributes)
        {
            if (!PayloadReaders.TryGetValue(pendingAttribute.Header.ClassHash, out var payloadReader))
            {
                throw new NotSupportedException(
                    $"AttribSys class 0x{pendingAttribute.Header.ClassHash:X16} ({pendingAttribute.Header.ClassName}) is not implemented.");
            }

            Dictionary<string, object> record = new()
            {
                ["header"] = pendingAttribute.Header,
                ["info"] = pendingAttribute.Info,
            };

            Dictionary<string, object> payload = payloadReader(reader);
            foreach (var kvp in payload)
            {
                record[kvp.Key] = kvp.Value;
            }

            Attributes.Add(record);
        }

        long remaining = reader.BaseStream.Length - reader.BaseStream.Position;
        Data = remaining > 0
            ? Convert.ToBase64String(reader.ReadBytes((int)remaining))
            : string.Empty;
    }

    private static string ReadFourCc(BinaryReader reader)
    {
        return Encoding.ASCII.GetString(reader.ReadBytes(4));
    }

    private static string ReadFixedLengthString(BinaryReader reader, int size)
    {
        StringBuilder builder = new();
        bool foundNull = false;
        for (int i = 0; i < size; i++)
        {
            char c = (char)reader.ReadByte();
            if (c == '\0')
            {
                foundNull = true;
            }

            if (!foundNull)
            {
                builder.Append(c);
            }
        }

        return builder.ToString();
    }

    private static string ReadCString(BinaryReader reader)
    {
        List<byte> bytes = [];
        while (reader.BaseStream.Position < reader.BaseStream.Length)
        {
            byte value = reader.ReadByte();
            if (value == 0)
            {
                break;
            }

            bytes.Add(value);
        }

        return Encoding.UTF8.GetString(bytes.ToArray());
    }

    private static void Align(EndianAwareBinaryReader reader, int alignment)
    {
        long position = reader.BaseStream.Position;
        long remainder = position % alignment;
        if (remainder == 0)
        {
            return;
        }

        reader.BaseStream.Position += alignment - remainder;
    }

    private static void SkipPadding(EndianAwareBinaryReader reader, int alignment)
    {
        Align(reader, alignment);
    }

    private static Vector4 ReadVector4(EndianAwareBinaryReader reader)
    {
        return new Vector4(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
    }

    private static string ReadStringRef(BinaryReader reader)
    {
        return Convert.ToBase64String(reader.ReadBytes(8));
    }

    private static AttribRefSpec ReadRefSpec(EndianAwareBinaryReader reader)
    {
        AttribRefSpec value = new()
        {
            ClassKey = reader.ReadUInt64(),
            CollectionKey = reader.ReadUInt64(),
            CollectionPtr = reader.ReadUInt32(),
        };
        reader.BaseStream.Seek(0x4, SeekOrigin.Current);
        return value;
    }

    private static Dictionary<string, object> ReadPhysicsVehicleEngineAttribs(EndianAwareBinaryReader reader)
    {
        return new Dictionary<string, object>
        {
            ["TorqueScales2"] = ReadVector4(reader),
            ["TorqueScales1"] = ReadVector4(reader),
            ["GearUpRPMs2"] = ReadVector4(reader),
            ["GearUpRPMs1"] = ReadVector4(reader),
            ["GearRatios2"] = ReadVector4(reader),
            ["GearRatios1"] = ReadVector4(reader),
            ["TransmissionEfficiency"] = reader.ReadSingle(),
            ["TorqueFallOffRPM"] = reader.ReadSingle(),
            ["MaxTorque"] = reader.ReadSingle(),
            ["MaxRPM"] = reader.ReadSingle(),
            ["LSDMGearUpSpeed"] = reader.ReadSingle(),
            ["GearChangeTime"] = reader.ReadSingle(),
            ["FlyWheelInertia"] = reader.ReadSingle(),
            ["FlyWheelFriction"] = reader.ReadSingle(),
            ["EngineResistance"] = reader.ReadSingle(),
            ["EngineLowEndTorqueFactor"] = reader.ReadSingle(),
            ["EngineBraking"] = reader.ReadSingle(),
            ["Differential"] = reader.ReadSingle(),
        };
    }

    private static Dictionary<string, object> ReadPhysicsVehicleDriftAttribs(EndianAwareBinaryReader reader)
    {
        Dictionary<string, object> value = new()
        {
            ["DriftScaleToYawTorque"] = ReadVector4(reader),
            ["WheelSlip"] = reader.ReadSingle(),
            ["TimeToCapScale"] = reader.ReadSingle(),
            ["TimeForNaturalDrift"] = reader.ReadSingle(),
            ["SteeringDriftScaleFactor"] = reader.ReadSingle(),
            ["SideForcePeakDriftAngle"] = reader.ReadSingle(),
            ["SideForceMagnitude"] = reader.ReadSingle(),
            ["SideForceDriftSpeedCutOff"] = reader.ReadSingle(),
            ["SideForceDriftAngleCutOff"] = reader.ReadSingle(),
            ["SideForceDirftScaleCutOff"] = reader.ReadSingle(),
            ["NeutralTimeToReduceDrift"] = reader.ReadSingle(),
            ["NaturalYawTorqueCutOffAngle"] = reader.ReadSingle(),
            ["NaturalYawTorque"] = reader.ReadSingle(),
            ["NaturalDriftTimeToReachBaseSlip"] = reader.ReadSingle(),
            ["NaturalDriftStartSlip"] = reader.ReadSingle(),
            ["NaturalDriftScaleDecay"] = reader.ReadSingle(),
            ["MinSpeedForDrift"] = reader.ReadSingle(),
            ["InitialDriftPushTime"] = reader.ReadSingle(),
            ["InitialDriftPushScaleLimit"] = reader.ReadSingle(),
            ["InitialDriftPushDynamicInc"] = reader.ReadSingle(),
            ["InitialDriftPushBaseInc"] = reader.ReadSingle(),
            ["GripFromSteering"] = reader.ReadSingle(),
            ["GripFromGasLetOff"] = reader.ReadSingle(),
            ["GripFromBrake"] = reader.ReadSingle(),
            ["GasDriftScaleFactor"] = reader.ReadSingle(),
            ["ForcedDriftTimeToReachBaseSlip"] = reader.ReadSingle(),
            ["ForcedDriftStartSlip"] = reader.ReadSingle(),
            ["DriftTorqueFallOff"] = reader.ReadSingle(),
            ["DriftSidewaysDamping"] = reader.ReadSingle(),
            ["DriftMaxAngle"] = reader.ReadSingle(),
            ["DriftAngularDamping"] = reader.ReadSingle(),
            ["CounterSteeringDriftScaleFactor"] = reader.ReadSingle(),
            ["CappedScale"] = reader.ReadSingle(),
            ["BrakingDriftScaleFactor"] = reader.ReadSingle(),
            ["BaseCounterSteeringDriftScaleFactor"] = reader.ReadSingle(),
        };
        SkipPadding(reader, 0x10);
        return value;
    }

    private static Dictionary<string, object> ReadPhysicsVehicleCollisionAttribs(EndianAwareBinaryReader reader)
    {
        return new Dictionary<string, object>
        {
            ["BodyBox"] = ReadVector4(reader),
        };
    }

    private static Dictionary<string, object> ReadPhysicsVehicleSuspensionAttribs(EndianAwareBinaryReader reader)
    {
        return new Dictionary<string, object>
        {
            ["UpwardMovement"] = reader.ReadSingle(),
            ["TimeToDampAfterLanding"] = reader.ReadSingle(),
            ["Strength"] = reader.ReadSingle(),
            ["SpringLength"] = reader.ReadSingle(),
            ["RearHeight"] = reader.ReadSingle(),
            ["MaxYawDampingOnLanding"] = reader.ReadSingle(),
            ["MaxVertVelocityDampingOnLanding"] = reader.ReadSingle(),
            ["MaxRollDampingOnLanding"] = reader.ReadSingle(),
            ["MaxPitchDampingOnLanding"] = reader.ReadSingle(),
            ["InAirDamping"] = reader.ReadSingle(),
            ["FrontHeight"] = reader.ReadSingle(),
            ["DownwardMovement"] = reader.ReadSingle(),
            ["Dampening"] = reader.ReadSingle(),
        };
    }

    private static Dictionary<string, object> ReadPhysicsVehicleSteeringAttribs(EndianAwareBinaryReader reader)
    {
        Dictionary<string, object> value = new()
        {
            ["TimeForLock"] = reader.ReadSingle(),
            ["StraightReactionBias"] = reader.ReadSingle(),
            ["SpeedForMinAngle"] = reader.ReadSingle(),
            ["SpeedForMaxAngle"] = reader.ReadSingle(),
            ["MinAngle"] = reader.ReadSingle(),
            ["MaxAngle"] = reader.ReadSingle(),
            ["AiPidCoefficientP"] = reader.ReadSingle(),
            ["AiPidCoefficientI"] = reader.ReadSingle(),
            ["AiPidCoefficientDriftP"] = reader.ReadSingle(),
            ["AiPidCoefficientDriftI"] = reader.ReadSingle(),
            ["AiPidCoefficientDriftD"] = reader.ReadSingle(),
            ["AiPidCoefficientD"] = reader.ReadSingle(),
            ["AiMinLookAheadDistanceForDrift"] = reader.ReadSingle(),
            ["AiLookAheadTimeForDrift"] = reader.ReadSingle(),
        };
        SkipPadding(reader, 0x10);
        return value;
    }

    private static Dictionary<string, object> ReadPhysicsVehicleHandlingAttribs(EndianAwareBinaryReader reader)
    {
        return new Dictionary<string, object>
        {
            ["PhysicsVehicleSuspensionAttribs"] = ReadRefSpec(reader),
            ["PhysicsVehicleSteeringAttribs"] = ReadRefSpec(reader),
            ["PhysicsVehicleEngineAttribs"] = ReadRefSpec(reader),
            ["PhysicsVehicleDriftAttribs"] = ReadRefSpec(reader),
            ["PhysicsVehicleCollisionAttribs"] = ReadRefSpec(reader),
            ["PhysicsVehicleBoostAttribs"] = ReadRefSpec(reader),
            ["PhysicsVehicleBodyRollAttribs"] = ReadRefSpec(reader),
            ["PhysicsVehicleBaseAttribs"] = ReadRefSpec(reader),
        };
    }

    private static Dictionary<string, object> ReadPhysicsVehicleBoostAttribs(EndianAwareBinaryReader reader)
    {
        return new Dictionary<string, object>
        {
            ["MaxBoostSpeed"] = reader.ReadSingle(),
            ["BoostRule"] = reader.ReadInt32(),
            ["BoostKickTime"] = reader.ReadSingle(),
            ["BoostKickMinTime"] = reader.ReadSingle(),
            ["BoostKickMaxTime"] = reader.ReadSingle(),
            ["BoostKickMaxStartSpeed"] = reader.ReadSingle(),
            ["BoostKickHeightOffset"] = reader.ReadSingle(),
            ["BoostKickAcceleration"] = reader.ReadSingle(),
            ["BoostKick"] = reader.ReadSingle(),
            ["BoostHeightOffset"] = reader.ReadSingle(),
            ["BoostBase"] = reader.ReadSingle(),
            ["BoostAcceleration"] = reader.ReadSingle(),
            ["BlueMaxBoostSpeed"] = reader.ReadSingle(),
            ["BlueBoostKickTime"] = reader.ReadSingle(),
            ["BlueBoostKick"] = reader.ReadSingle(),
            ["BlueBoostBase"] = reader.ReadSingle(),
        };
    }

    private static Dictionary<string, object> ReadCameraBumperBehaviourAttribs(EndianAwareBinaryReader reader)
    {
        return new Dictionary<string, object>
        {
            ["ZOffset"] = reader.ReadSingle(),
            ["YOffset"] = reader.ReadSingle(),
            ["YawSpring"] = reader.ReadSingle(),
            ["RollSpring"] = reader.ReadSingle(),
            ["PitchSpring"] = reader.ReadSingle(),
            ["FieldOfView"] = reader.ReadSingle(),
            ["BoostFieldOfView"] = reader.ReadSingle(),
            ["BodyRollScale"] = reader.ReadSingle(),
            ["BodyPitchScale"] = reader.ReadSingle(),
            ["AccelerationResponse"] = reader.ReadSingle(),
            ["AccelerationDampening"] = reader.ReadSingle(),
        };
    }

    private static Dictionary<string, object> ReadCameraExternalBehaviourAttribs(EndianAwareBinaryReader reader)
    {
        return new Dictionary<string, object>
        {
            ["ZDistanceScale"] = reader.ReadSingle(),
            ["ZAndTiltCutoffSpeedMPH"] = reader.ReadSingle(),
            ["YawSpring"] = reader.ReadSingle(),
            ["TiltCameraScale"] = reader.ReadSingle(),
            ["TiltAroundCar"] = reader.ReadSingle(),
            ["SlideZOffsetMax"] = reader.ReadSingle(),
            ["SlideYScale"] = reader.ReadSingle(),
            ["SlideXScale"] = reader.ReadSingle(),
            ["PivotZOffset"] = reader.ReadSingle(),
            ["PivotLength"] = reader.ReadSingle(),
            ["PivotHeight"] = reader.ReadSingle(),
            ["PitchSpring"] = reader.ReadSingle(),
            ["FieldOfView"] = reader.ReadSingle(),
            ["DriftYawSpring"] = reader.ReadSingle(),
            ["DownAngle"] = reader.ReadSingle(),
            ["BoostFieldOfViewZoom"] = reader.ReadSingle(),
            ["BoostFieldOfView"] = reader.ReadSingle(),
        };
    }

    private static Dictionary<string, object> ReadBurnoutCarGraphicsAssetAttribs(EndianAwareBinaryReader reader)
    {
        int playerPalletteIndex = reader.ReadInt32();
        int playerColourIndex = reader.ReadInt32();
        short alloc = reader.ReadInt16();
        short numRandomTrafficColours = reader.ReadInt16();
        short size = reader.ReadInt16();
        short encodedTypePad = reader.ReadInt16();

        List<int> randomTrafficColours = [];
        for (int i = 0; i < numRandomTrafficColours; i++)
        {
            randomTrafficColours.Add(reader.ReadInt32());
        }

        for (int i = numRandomTrafficColours; i < alloc; i++)
        {
            reader.BaseStream.Seek(0x4, SeekOrigin.Current);
        }

        return new Dictionary<string, object>
        {
            ["PlayerPalletteIndex"] = playerPalletteIndex,
            ["PlayerColourIndex"] = playerColourIndex,
            ["Alloc"] = alloc,
            ["Num_RandomTrafficColours"] = numRandomTrafficColours,
            ["Size"] = size,
            ["EncodedTypePad"] = encodedTypePad,
            ["RandomTrafficColours"] = randomTrafficColours,
            ["Alloc_Offences"] = reader.ReadInt16(),
            ["Num_Offences"] = reader.ReadInt16(),
            ["Size_Offences"] = reader.ReadInt16(),
            ["EncodedTypePad_Offences"] = reader.ReadInt16(),
        };
    }

    private static Dictionary<string, object> ReadPhysicsVehicleBodyRollAttribs(EndianAwareBinaryReader reader)
    {
        Dictionary<string, object> value = new()
        {
            ["WheelLongForceHeightOffset"] = reader.ReadSingle(),
            ["WheelLatForceHeightOffset"] = reader.ReadSingle(),
            ["WeightTransferDecayZ"] = reader.ReadSingle(),
            ["WeightTransferDecayX"] = reader.ReadSingle(),
            ["RollSpringStiffness"] = reader.ReadSingle(),
            ["RollSpringDampening"] = reader.ReadSingle(),
            ["PitchSpringStiffness"] = reader.ReadSingle(),
            ["PitchSpringDampening"] = reader.ReadSingle(),
            ["FactorOfWeightZ"] = reader.ReadSingle(),
            ["FactorOfWeightX"] = reader.ReadSingle(),
        };
        reader.BaseStream.Seek(0x4, SeekOrigin.Current);
        return value;
    }

    private static Dictionary<string, object> ReadPhysicsVehicleBaseAttribs(EndianAwareBinaryReader reader)
    {
        int start = (int)reader.BaseStream.Position;
        Align(reader, 0x10);
        int paddingLength = (int)reader.BaseStream.Position - start;

        return new Dictionary<string, object>
        {
            ["RearRightWheelPosition"] = ReadVector4(reader),
            ["FrontRightWheelPosition"] = ReadVector4(reader),
            ["CoMOffset"] = ReadVector4(reader),
            ["BrakeScaleToFactor"] = ReadVector4(reader),
            ["YawDampingOnTakeOff"] = reader.ReadSingle(),
            ["TractionLineLength"] = reader.ReadSingle(),
            ["TimeForFullBrake"] = reader.ReadSingle(),
            ["SurfaceRoughnessFactor"] = reader.ReadSingle(),
            ["SurfaceRearGripFactor"] = reader.ReadSingle(),
            ["SurfaceFrontGripFactor"] = reader.ReadSingle(),
            ["SurfaceDragFactor"] = reader.ReadSingle(),
            ["RollLimitOnTakeOff"] = reader.ReadSingle(),
            ["RollDampingOnTakeOff"] = reader.ReadSingle(),
            ["RearWheelMass"] = reader.ReadSingle(),
            ["RearTireStaticFrictionCoefficient"] = reader.ReadSingle(),
            ["RearTireLongForceBias"] = reader.ReadSingle(),
            ["RearTireDynamicFrictionCoefficient"] = reader.ReadSingle(),
            ["RearTireAdhesiveLimit"] = reader.ReadSingle(),
            ["RearLongGripCurvePeakSlipRatio"] = reader.ReadSingle(),
            ["RearLongGripCurvePeakCoefficient"] = reader.ReadSingle(),
            ["RearLongGripCurveFloorSlipRatio"] = reader.ReadSingle(),
            ["RearLongGripCurveFallCoefficient"] = reader.ReadSingle(),
            ["RearLatGripCurvePeakSlipRatio"] = reader.ReadSingle(),
            ["RearLatGripCurvePeakCoefficient"] = reader.ReadSingle(),
            ["RearLatGripCurveFloorSlipRatio"] = reader.ReadSingle(),
            ["RearLatGripCurveFallCoefficient"] = reader.ReadSingle(),
            ["RearLatGripCurveDriftPeakSlipRatio"] = reader.ReadSingle(),
            ["PowerToRear"] = reader.ReadSingle(),
            ["PowerToFront"] = reader.ReadSingle(),
            ["PitchDampingOnTakeOff"] = reader.ReadSingle(),
            ["MaxSpeed"] = reader.ReadSingle(),
            ["MagicBrakeFactorTurning"] = reader.ReadSingle(),
            ["MagicBrakeFactorStraightLine"] = reader.ReadSingle(),
            ["LowSpeedTyreFrictionTractionControl"] = reader.ReadSingle(),
            ["LowSpeedThrottleTractionControl"] = reader.ReadSingle(),
            ["LowSpeedDrivingSpeed"] = reader.ReadSingle(),
            ["LockBrakeScale"] = reader.ReadSingle(),
            ["LinearDrag"] = reader.ReadSingle(),
            ["HighSpeedAngularDamping"] = reader.ReadSingle(),
            ["FrontWheelMass"] = reader.ReadSingle(),
            ["FrontTireStaticFrictionCoefficient"] = reader.ReadSingle(),
            ["FrontTireLongForceBias"] = reader.ReadSingle(),
            ["FrontTireDynamicFrictionCoefficient"] = reader.ReadSingle(),
            ["FrontTireAdhesiveLimit"] = reader.ReadSingle(),
            ["FrontLongGripCurvePeakSlipRatio"] = reader.ReadSingle(),
            ["FrontLongGripCurvePeakCoefficient"] = reader.ReadSingle(),
            ["FrontLongGripCurveFloorSlipRatio"] = reader.ReadSingle(),
            ["FrontLongGripCurveFallCoefficient"] = reader.ReadSingle(),
            ["FrontLatGripCurvePeakSlipRatio"] = reader.ReadSingle(),
            ["FrontLatGripCurvePeakCoefficient"] = reader.ReadSingle(),
            ["FrontLatGripCurveFloorSlipRatio"] = reader.ReadSingle(),
            ["FrontLatGripCurveFallCoefficient"] = reader.ReadSingle(),
            ["FrontLatGripCurveDriftPeakSlipRatio"] = reader.ReadSingle(),
            ["DrivingMass"] = reader.ReadSingle(),
            ["DriveTimeDeformLimitX"] = reader.ReadSingle(),
            ["DriveTimeDeformLimitPosZ"] = reader.ReadSingle(),
            ["DriveTimeDeformLimitNegZ"] = reader.ReadSingle(),
            ["DriveTimeDeformLimitNegY"] = reader.ReadSingle(),
            ["DownForceZOffset"] = reader.ReadSingle(),
            ["DownForce"] = reader.ReadSingle(),
            ["CrashExtraYawVelocityFactor"] = reader.ReadSingle(),
            ["CrashExtraRollVelocityFactor"] = reader.ReadSingle(),
            ["CrashExtraPitchVelocityFactor"] = reader.ReadSingle(),
            ["CrashExtraLinearVelocityFactor"] = reader.ReadSingle(),
            ["AngularDrag"] = reader.ReadSingle(),
            ["PaddingLength"] = paddingLength,
        };
    }

    private static Dictionary<string, object> ReadBurnoutCarAssetAttribs(EndianAwareBinaryReader reader)
    {
        List<AttribRefSpec> offences = [];
        for (int i = 0; i < 12; i++)
        {
            offences.Add(ReadRefSpec(reader));
        }

        AttribRefSpec soundExhaustAsset = ReadRefSpec(reader);
        AttribRefSpec soundEngineAsset = ReadRefSpec(reader);
        AttribRefSpec physicsVehicleHandlingAsset = ReadRefSpec(reader);
        AttribRefSpec graphicsAsset = ReadRefSpec(reader);
        AttribRefSpec carUnlockShot = ReadRefSpec(reader);
        AttribRefSpec cameraExternalBehaviourAsset = ReadRefSpec(reader);
        AttribRefSpec cameraBumperBehaviourAsset = ReadRefSpec(reader);

        string vehicleId = ReadStringRef(reader);
        long physicsAsset = reader.ReadInt64();
        long masterSceneMayaBinaryFile = reader.ReadInt64();
        string inGameName = ReadStringRef(reader);
        long gameplayAsset = reader.ReadInt64();
        string exhaustName = ReadStringRef(reader);
        long exhaustEntityKey = reader.ReadInt64();
        string engineName = ReadStringRef(reader);
        long engineEntityKey = reader.ReadInt64();
        long defaultWheel = reader.ReadInt64();
        bool buildThisVehicle = reader.ReadBoolean();
        reader.BaseStream.Seek(0x3, SeekOrigin.Current);

        return new Dictionary<string, object>
        {
            ["VehicleID"] = vehicleId,
            ["InGameName"] = inGameName,
            ["ExhaustName"] = exhaustName,
            ["EngineName"] = engineName,
            ["Offences"] = offences,
            ["SoundExhaustAsset"] = soundExhaustAsset,
            ["SoundEngineAsset"] = soundEngineAsset,
            ["PhysicsVehicleHandlingAsset"] = physicsVehicleHandlingAsset,
            ["GraphicsAsset"] = graphicsAsset,
            ["CarUnlockShot"] = carUnlockShot,
            ["CameraExternalBehaviourAsset"] = cameraExternalBehaviourAsset,
            ["CameraBumperBehaviourAsset"] = cameraBumperBehaviourAsset,
            ["PhysicsAsset"] = physicsAsset,
            ["MasterSceneMayaBinaryFile"] = masterSceneMayaBinaryFile,
            ["GameplayAsset"] = gameplayAsset,
            ["ExhaustEntityKey"] = exhaustEntityKey,
            ["EngineEntityKey"] = engineEntityKey,
            ["DefaultWheel"] = defaultWheel,
            ["BuildThisVehicle"] = buildThisVehicle,
        };
    }

    private sealed class PendingAttribute
    {
        public required AttribAttributeHeader Header { get; init; }
        public required AttribChunkInfo Info { get; init; }
    }
}

public sealed class AttribChunkInfo
{
    public ulong Hash { get; set; }
    public ulong EntryTypeHash { get; set; }
    public int DataChunkSize { get; set; }
    public int DataChunkPosition { get; set; }
}

public sealed class AttribAttributeHeader
{
    public string ClassName { get; set; } = string.Empty;
    public ulong CollectionHash { get; set; }
    public ulong ClassHash { get; set; }
    public string Unknown1 { get; set; } = string.Empty;
    public int ItemCount { get; set; }
    public int Unknown2 { get; set; }
    public int ItemCountDup { get; set; }
    public short ParameterCount { get; set; }
    public short ParametersToRead { get; set; }
    public string Unknown3 { get; set; } = string.Empty;
    public ulong[] ParameterTypeHashes { get; set; } = [];
    public List<AttribDataItem> Items { get; set; } = [];
}

public sealed class AttribDataItem
{
    public ulong Hash { get; set; }
    public string Unknown1 { get; set; } = string.Empty;
    public short ParameterIdx { get; set; }
    public short Unknown2 { get; set; }
}

public sealed class AttribPointerChunkData
{
    public uint Ptr { get; set; }
    public short Type { get; set; }
    public short Flag { get; set; }
    public ulong Data { get; set; }
}

public sealed class AttribRefSpec
{
    public ulong ClassKey { get; set; }
    public ulong CollectionKey { get; set; }
    public uint CollectionPtr { get; set; }
}
