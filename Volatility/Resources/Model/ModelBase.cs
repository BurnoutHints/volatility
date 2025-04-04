namespace Volatility.Resources.Model;

// The Model resource type links top-level resources (like InstanceList)
// to Renderable resources that contain the 3D geometry, while including
// Level of Detail (LOD) information.

// Learn More:
// https://burnout.wiki/wiki/Model

public class ModelBase : Resource
{
    [EditorCategory("Model Container"), EditorLabel("Flags")]
    public byte Flags;

    [EditorCategory("Model Container"), EditorLabel("Models")]
    public List<ModelData> ModelDatas = new List<ModelData>();

    public override ResourceType GetResourceType() => ResourceType.Model;
    public override Platform GetResourcePlatform() => Platform.Agnostic;

    public override void WriteToStream(EndianAwareBinaryWriter writer)
    {
        base.WriteToStream(writer);

        int models = ModelDatas.Count();

        uint renderablesPtr = 0x14; // Writing length of header
        uint statesPtr = (uint)(renderablesPtr + (0x4 * models));
        uint lodDistancesPtr = (uint)(statesPtr + (0x1 * models));

        writer.Write(renderablesPtr);
        writer.Write(statesPtr);
        writer.Write(lodDistancesPtr);
        writer.Write(-1); // Game explorer index, leaving our mark for now
        writer.Write((byte)ModelDatas.Count()); // Dangerous. We need to limit number of models
        writer.Write(Flags);
        writer.Write((byte)ModelDatas.Count()); // Number of states. Same as number of renderables
        writer.Write((byte)0x2);

        writer.BaseStream.Seek(renderablesPtr, SeekOrigin.Begin);

        // Renderable Ptrs
        for (int i = 0; i < models; i++)
        {
            writer.Write((uint)(i * 0x4));
        }

        // States (Writing as uint?? A single renderable has a 0x4-long state apparently.)
        for (int i = 0; i < models; i++)
        {
            writer.Write((uint)ModelDatas[i].State);
        }

        // LOD Distances
        for (int i = 0; i < models; i++)
        {
            writer.Write(ModelDatas[i].LODDistance);
        }

        // Resource ID References
        for (int i = 0; i < models; i++)
        {
            writer.Write(ModelDatas[i].ResourceReference.Endian == Endian.BE ? new byte[4] : ModelDatas[i].ResourceReference.ID);
            writer.Write(ModelDatas[i].ResourceReference.Endian == Endian.LE ? new byte[4] : ModelDatas[i].ResourceReference.ID);
            writer.Write(renderablesPtr + (i * 0x4));
            writer.Write((uint)0x0); // Unknown. Always 0 in BPR, not always 0 on X360
        }
    }
    public override void ParseFromStream(ResourceBinaryReader reader)
    {
        base.ParseFromStream(reader);

        // Get the version check out of the way before we begin.
        reader.BaseStream.Seek(0x13, SeekOrigin.Begin);
        if (reader.ReadByte() != 0x2)
        {
            throw new Exception("Version mismatch!");
        }

        reader.BaseStream.Seek(0x0, SeekOrigin.Begin);

        // Absolute pointers (not relative to any specific point in the file)
        uint renderablesPtr = reader.ReadUInt32();
        uint renderableStatesPtr = reader.ReadUInt32();
        uint lodDistancesPtr = reader.ReadUInt32();

        // Null for imported resources.
        // TODO: Reconstruct game explorer or get from ResourceDB
        int gameExplorerIndex = reader.ReadInt32();

        byte numRenderables = reader.ReadByte();

        if (numRenderables == 0)
        {
            Console.WriteLine("WARNING: Found no renderables in this model!");
        }

        Flags = reader.ReadByte();

        // This currently does a lot of seeking.
        // It may improve performance if we separate this.
        for (uint i = 0; i < numRenderables; i++)
        {
            ModelData modelData = new ModelData();
            
            reader.BaseStream.Seek(renderablesPtr + (i * 0x4), SeekOrigin.Begin);

            uint idRelativePtr = reader.ReadUInt32();

            reader.BaseStream.Seek(renderableStatesPtr + i, SeekOrigin.Begin);
            modelData.State = (State)reader.ReadByte();

            reader.BaseStream.Seek(lodDistancesPtr + (i * 0x4), SeekOrigin.Begin);
            modelData.LODDistance = reader.ReadSingle();

            reader.BaseStream.Seek(
                idRelativePtr + 
                renderablesPtr + 
                (numRenderables * (0x4 + 0x1 + 0x4)) + 
                (reader.GetEndianness() == Endian.BE ? 0x4 : 0x0), SeekOrigin.Begin
            );

            modelData.ResourceReference.ID = reader.ReadBytes(4);
            modelData.ResourceReference.Endian = reader.GetEndianness();

            ModelDatas.Add(modelData);
        }
    }

    public ModelBase() :base() { }

    public ModelBase(string path) : base(path) { }

    public struct ModelData
    {
        [EditorCategory("Model Data"), EditorLabel("Resource Reference")]
        public ResourceID ResourceReference;

        [EditorCategory("Model Data"), EditorLabel("Model State")]
        public State State;

        [EditorCategory("Model Data"), EditorLabel("LOD Render Distance")]
        public float LODDistance;
    }

    public enum State : byte
    {
        [EditorLabel("LOD 0")]
        E_STATE_LOD_0 = 0,
        [EditorLabel("LOD 1")]
        E_STATE_LOD_1 = 1,
        [EditorLabel("LOD 2")]
        E_STATE_LOD_2 = 2,
        [EditorLabel("LOD 3")]
        E_STATE_LOD_3 = 3,
        [EditorLabel("LOD 4")]
        E_STATE_LOD_4 = 4,
        [EditorLabel("LOD 5")]
        E_STATE_LOD_5 = 5,
        [EditorLabel("LOD 6")]
        E_STATE_LOD_6 = 6,
        [EditorLabel("LOD 7")]
        E_STATE_LOD_7 = 7,
        [EditorLabel("LOD 8")]
        E_STATE_LOD_8 = 8,
        [EditorLabel("LOD 9")]
        E_STATE_LOD_9 = 9,
        [EditorLabel("LOD 10")]
        E_STATE_LOD_10 = 10,
        [EditorLabel("LOD 11")]
        E_STATE_LOD_11 = 11,
        [EditorLabel("LOD 12")]
        E_STATE_LOD_12 = 12,
        [EditorLabel("LOD 13")]
        E_STATE_LOD_13 = 13,
        [EditorLabel("LOD 14")]
        E_STATE_LOD_14 = 14,
        [EditorLabel("LOD 15")]
        E_STATE_LOD_15 = 15,
        [EditorLabel("Game Specific 0")]
        E_STATE_GAME_SPECIFIC_0 = 16,
        [EditorLabel("Game Specific 1")]
        E_STATE_GAME_SPECIFIC_1 = 17,
        [EditorLabel("Game Specific 2")]
        E_STATE_GAME_SPECIFIC_2 = 18,
        [EditorLabel("Game Specific 3")]
        E_STATE_GAME_SPECIFIC_3 = 19,
        [EditorLabel("Game Specific 4")]
        E_STATE_GAME_SPECIFIC_4 = 20,
        [EditorLabel("Game Specific 5")]
        E_STATE_GAME_SPECIFIC_5 = 21,
        [EditorLabel("Game Specific 6")]
        E_STATE_GAME_SPECIFIC_6 = 22,
        [EditorLabel("Game Specific 7")]
        E_STATE_GAME_SPECIFIC_7 = 23,
        [EditorLabel("Game Specific 8")]
        E_STATE_GAME_SPECIFIC_8 = 24,
        [EditorLabel("Game Specific 9")]
        E_STATE_GAME_SPECIFIC_9 = 25,
        [EditorLabel("Game Specific 10")]
        E_STATE_GAME_SPECIFIC_10 = 26,
        [EditorLabel("Game Specific 11")]
        E_STATE_GAME_SPECIFIC_11 = 27,
        [EditorLabel("Game Specific 12")]
        E_STATE_GAME_SPECIFIC_12 = 28,
        [EditorLabel("Game Specific 13")]
        E_STATE_GAME_SPECIFIC_13 = 29,
        [EditorLabel("Game Specific 14")]
        E_STATE_GAME_SPECIFIC_14 = 30,
        [EditorLabel("Game Specific 15")]
        E_STATE_GAME_SPECIFIC_15 = 31,
        [EditorLabel("Invalid")]
        E_STATE_INVALID = 32,
        [EditorHidden]
        E_STATE_COUNT = 32,
    }
}
