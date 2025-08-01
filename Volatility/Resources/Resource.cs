﻿using static Volatility.Utilities.ResourceIDUtilities;

namespace Volatility.Resources;

public abstract class Resource
{
    [EditorCategory("Resource Info"), EditorLabel("Resource ID"), EditorTooltip("The CRC32 ID used to identify the resource in the game engine.")]
    public ResourceID ResourceID = ResourceID.Default;

    [EditorCategory("Resource Info"), EditorLabel("Asset Name"), EditorTooltip("The asset's name in the resource depot.")]
    public string AssetName = "invalid";

    [EditorCategory("Resource Info"), EditorLabel("Platform Architecture"), EditorTooltip("Forced platform architecture. Determines field sizes such as pointers. Console versions of Remastered use x64 while other known platforms typically use x32.")]
    public Arch Arch = Arch.x32;

    [EditorCategory("Import Data"), EditorLabel("Imported File Path"), EditorTooltip("The path that this resource was imported from.")]
    public string? ImportedFileName;

    [EditorCategory("Import Data"), EditorLabel("Unpacker"), EditorTooltip("The tool used to extract this resource from a bundle.")]
    public Unpacker Unpacker = Unpacker.Raw;

    public virtual ResourceType GetResourceType() => ResourceType.Invalid;
    public virtual Endian GetResourceEndian() => Endian.Agnostic;   // Forced endianness for platform-specific resources (e.g. Textures)
    public virtual Platform GetResourcePlatform() => Platform.Agnostic;
    public virtual Arch GetResourceArch() => Arch;
    public virtual void SetResourceArch(Arch newArch) { Arch = newArch; }

    public virtual void WriteToStream(EndianAwareBinaryWriter writer, Endian endianness = Endian.Agnostic) 
    { 
        if (GetResourceEndian() != Endian.Agnostic)
            writer.SetEndianness(GetResourceEndian());

        else if (endianness != Endian.Agnostic)
            writer.SetEndianness(endianness);
    }
    public virtual void ParseFromStream(ResourceBinaryReader reader, Endian endianness = Endian.Agnostic) 
    {
        if (GetResourceEndian() != Endian.Agnostic)
            reader.SetEndianness(GetResourceEndian());

        else if (endianness != Endian.Agnostic)
            reader.SetEndianness(endianness);
    }

    public Resource() { }

    public Resource(string path, Endian endianness = Endian.Agnostic)
    {
        if (string.IsNullOrEmpty(path))
            return;

        ImportedFileName = path;

        // Don't parse a directory
        if (new DirectoryInfo(path).Exists)
            return;

        string? name = Path.GetFileNameWithoutExtension(ImportedFileName);

        Endian importEndianness = (GetResourceEndian() != Endian.Agnostic) ? GetResourceEndian() : endianness;

        if (!string.IsNullOrEmpty(name))
        {
            // If the filename is a ResourceID, we scan the users' ResourceDB (if available)
            // to find a matching asset name for the provided ResourceID. If none is found,
            // the ResourceID is used in place of a real asset name. If the filename is not a
            // ResourceID, we simply use the file name as the asset name, and calculate a new ResourceID.
            Unpacker = GetUnpackerFromFileName(Path.GetFileName(ImportedFileName));
            if (Unpacker != Unpacker.Raw)
            {
                int idx = name.LastIndexOf('_');
                name = Unpacker switch
                {
                    Unpacker.DGI => name.Replace("_", ""),
                    Unpacker.Bnd2Manager or Unpacker.YAP when idx > 0 => name[..idx],
                    Unpacker.Bnd2Manager or Unpacker.YAP => name,                    
                    _ => name
                };
            }
            if (ValidateResourceID(name))
            {
                // We store ResourceIDs how BE platforms do to be consistent with the original console releases.
                // This makes it easy to cross reference assets between all platforms.
                ResourceID = Convert.ToUInt64((importEndianness == Endian.LE && Unpacker != Unpacker.YAP)
                    ? FlipResourceIDEndian(name)
                    : name
                    , 16);

                string newName = GetNameByResourceID(ResourceID);
                AssetName = !string.IsNullOrEmpty(newName)
                    ? newName
                    : ResourceID.ToString();
            }
            else
            {
                // TODO: Add new entry to ResourceDB
                ResourceID = Convert.ToUInt64(ResourceID.FromIDString(name));
                AssetName = name;
            }

        }

        using (ResourceBinaryReader reader = new ResourceBinaryReader(new FileStream($"{path}", FileMode.Open), importEndianness))
        {
            ParseFromStream(reader, importEndianness);
        }
    }

    private static Unpacker GetUnpackerFromFileName(string filename)
    {
        var name = Path.GetFileName(filename);
        return name switch
        {
            var n when n.EndsWith("_1.bin", StringComparison.OrdinalIgnoreCase) => Unpacker.Bnd2Manager,
            var n when n.EndsWith("_primary.dat", StringComparison.OrdinalIgnoreCase) => Unpacker.YAP,
            var n when n.EndsWith(".dat", StringComparison.OrdinalIgnoreCase)
                      && n.Count(c => c == '_') == 3 => Unpacker.DGI,
            var n when n.EndsWith(".dat", StringComparison.OrdinalIgnoreCase) => Unpacker.YAP,
            _ => Unpacker.Raw,
        };
    }

    public virtual void PushAll() { }
    public virtual void PullAll() { }
}


public enum ResourceType
{
    Texture = 0x0,
    Material = 0x1,
    RenderableMesh = 0x2,
    TextFile = 0x3,
    DrawIndexParams = 0x4,
    IndexBuffer = 0x5,
    MeshState = 0x6,
    TEXTUREAUXINFO = 0x7,
    VERTEXBUFFERITEM = 0x8,
    VertexBuffer = 0x9,
    VertexDescriptor = 0xA,
    RwMaterialCRC32 = 0xB,
    Renderable = 0xC,
    MaterialTechnique = 0xD,
    TextureState = 0xE,
    MaterialState = 0xF,
    DepthStencilState = 0x10,
    RasterizerState = 0x11,
    RwShaderProgramBuffer = 0x12,
    RenderTargetState = 0x13,
    RwShaderParameter = 0x14,
    RenderableAssembly = 0x15,
    RwDebug = 0x16,
    KdTree = 0x17,
    VoiceHierarchy = 0x18,
    Snr = 0x19,
    InterpreterData = 0x1A,
    AttribSysSchema = 0x1B,
    AttribSysVault = 0x1C,
    EntryList = 0x1D,
    AptData = 0x1E,
    GuiPopup = 0x1F,
    Font = 0x21,
    LuaCode = 0x22,
    InstanceList = 0x23,
    ClusteredMesh = 0x24,
    IdList = 0x25,
    InstanceCollisionList = 0x26,
    Language = 0x27,
    SatNavTile = 0x28,
    SatNavTileDirectory = 0x29,
    Model = 0x2A,
    ColourCube = 0x2B,
    HudMessage = 0x2C,
    HudMessageList = 0x2D,
    HudMessageSequence = 0x2E,
    HudMessageSequenceDictionary = 0x2F,
    WorldPainter2D = 0x30,
    PFXHookBundle = 0x31,
    ShaderTechnique = 0x32,
    Shader = 0x32,
    RawFile = 0x40,
    ICETakeDictionary = 0x41,
    VideoData = 0x42,
    PolygonSoupList = 0x43,
    DeveloperList = 0x44,
    CommsToolListDefinition = 0x45,
    CommsToolList = 0x46,
    BinaryFile = 0x50,
    AnimationCollection = 0x51,
    CharAnimBankFile = 0x2710,
    WeaponFile = 0x2711,
    VFXFile = 0x343E,
    BearFile = 0x343F,
    BkPropInstanceList = 0x3A98,
    Registry = 0xA000,
    GENERIC_RWAC_FACTORY_CONFIGURATION = 0xA010,
    GenericRwacWaveContent = 0xA020,
    GinsuWaveContent = 0xA021,
    AemsBank = 0xA022,
    Csis = 0xA023,
    Nicotine = 0xA024,
    Splicer = 0xA025,
    FreqContent = 0xA026,
    VoiceHierarchyCollection = 0xA027,
    GenericRwacReverbIRContent = 0xA028,
    SnapshotData = 0xA029,
    ZoneList = 0xB000,
    VFX = 0xC001,
    LoopModel = 0x10000,
    AISections = 0x10001,
    TrafficData = 0x10002,
    TriggerData = 0x10003,
    DeformationModel = 0x10004,
    VehicleList = 0x10005,
    GraphicsSpec = 0x10006,
    PhysicsSpec = 0x10007,
    ParticleDescriptionCollection = 0x10008,
    WheelList = 0x10009,
    WheelGraphicsSpec = 0x1000A,
    TextureNameMap = 0x1000B,
    ICEList = 0x1000C,
    ICEData = 0x1000D,
    ProgressionData = 0x1000E,
    PropPhysics = 0x1000F,
    PropGraphicsList = 0x10010,
    PropInstanceData = 0x10011,
    EnvironmentKeyframe = 0x10012,
    EnvironmentTimeLine = 0x10013,
    EnvironmentDictionary = 0x10014,
    GraphicsStub = 0x10015,
    StaticSoundMap = 0x10016,
    PFXHookBundle2 = 0x10017,
    StreetData = 0x10018,
    VFXMeshCollection = 0x10019,
    MassiveLookupTable = 0x1001A,
    VFXPropCollection = 0x1001B,
    StreamedDeformationSpec = 0x1001C,
    ParticleDescription = 0x1001D,
    PlayerCarColours = 0x1001E,
    ChallengeList = 0x1001F,
    FlaptFile = 0x10020,
    ProfileUpgrade = 0x10021,
    OfflineChallengeList = 0x10022,
    VehicleAnimation = 0x10023,
    BodypartRemapData = 0x10024,
    LUAList = 0x10025,
    LUAScript = 0x10026,
    BkSoundWeapon = 0x11000,
    BkSoundGunsu = 0x11001,
    BkSoundBulletImpact = 0x11002,
    BkSoundBulletImpactList = 0x11003,
    BkSoundBulletImpactStream = 0x11004,
    Invalid = 0xFFFFFF,
}

public enum Platform
{
    Agnostic = -1,
    BPR = 0,
    TUB = 1,
    X360 = 2,
    PS3 = 3,
}

public enum Unpacker 
{
    Raw = 0,
    Volatility = 1,
    Bnd2Manager = 2,
    DGI = 3,
    YAP = 4,
}

public enum Arch
{
    [EditorLabel("32 bit (Default)")]
    x32 = 0,
    [EditorLabel("64 bit (Console BPR)")]
    x64 = 1
}