﻿using static Volatility.Utilities.ResourceIDUtilities;

namespace Volatility.Resource;

public abstract class Resource
{
    public string ResourceID = "";
    public string AssetName = "invalid";
    public string? ImportPath;
    public static readonly ResourceType ResourceType;
    public static readonly Endian ResourceEndian;

    public virtual void WriteToStream(BinaryWriter writer) { }
    public virtual void ParseFromStream(BinaryReader reader) { }

    public Resource() { }

    public Resource(string path)
    {
        ImportPath = path;

        // Don't parse a directory
        if (new DirectoryInfo(path).Exists)
            return;

        string? name = Path.GetFileNameWithoutExtension(ImportPath);

        if (!string.IsNullOrEmpty(name))
        {
            // If the filename is a ResourceID, we scan the users' ResourceDB (if available)
            // to find a matching asset name for the provided ResourceID. If none is found,
            // the ResourceID is used in place of a real asset name. If the filename is not a
            // ResourceID, we simply use the file name as the asset name, and calculate a new ResourceID.
            if (ValidateResourceID(name))
            {
                name = name.Replace("_", "");

                // We store ResourceIDs how BE platforms do to be consistent with the original console releases.
                // This makes it easy to cross reference assets between all platforms.
                ResourceID = (ResourceEndian == Endian.LE)
                    ? FlipResourceIDEndian(name)
                    : name;

                string newName = GetNameByResourceID(ResourceID, ResourceType.ToString());
                AssetName = !string.IsNullOrEmpty(newName)
                    ? newName
                    : ResourceID;
            }
            else
            {
                // TODO: Add new entry to ResourceDB
                ResourceID = (ResourceEndian == Endian.LE)
                    ? GetResourceIDFromName(name)
                    : FlipResourceIDEndian(GetResourceIDFromName(name));

                AssetName = name;
            }

        }

        using (BinaryReader reader = new BinaryReader(new FileStream($"{path}", FileMode.Open)))
        {
            ParseFromStream(reader);
        }
    }
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
    BkSoundBulletImpactStream = 0x11004
}

public enum Endian
{
    LE,
    BE
}