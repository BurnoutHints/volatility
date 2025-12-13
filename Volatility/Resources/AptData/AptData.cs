using static Volatility.Utilities.DataUtilities;

namespace Volatility.Resources;

public class AptData : Resource
{
    public override ResourceType GetResourceType() => ResourceType.AptData;
    public override Platform GetResourcePlatform() => Platform.Agnostic;

    public string MovieName;
    public string BaseComponentName;
    public GuiGeometryObject GuiGeometry;

    public override void WriteToStream(EndianAwareBinaryWriter writer, Endian endianness = Endian.Agnostic)
    {
        base.WriteToStream(writer);
    }
    public override void ParseFromStream(ResourceBinaryReader reader, Endian endianness = Endian.Agnostic)
    {
        base.ParseFromStream(reader, endianness);

        long movieNamePtr = reader.ReadUInt32();
        long baseComponentNamePtr = reader.ReadUInt32();
        long aptDataPtr = reader.ReadUInt32();
        long constFilePtr = reader.ReadUInt32();
        long geomStructPtr = reader.ReadUInt32();
        uint sizedata = reader.ReadUInt32();
        _ = reader.ReadUInt32(); // EAptDataState meCurrentState

        // Gui Geometry
        reader.BaseStream.Position = geomStructPtr;

        uint numGeometryFiles = reader.ReadUInt32();
        uint numTexturePages = reader.ReadUInt32();

        List<GuiGeometryFile> guiGeometryFiles = [];

        uint geometryFilesPtr = reader.ReadUInt32();
        
        // GuiGeometryFile
        for (int i = 0; i < numGeometryFiles; i++) 
        {
            reader.BaseStream.Seek(geometryFilesPtr + (0x4 * i), SeekOrigin.Begin);
            reader.BaseStream.Seek(reader.ReadUInt32(), SeekOrigin.Begin);
           
            uint muID = reader.ReadUInt32();
            uint numMeshes = reader.ReadUInt32();
            uint geometryMeshesPtr = reader.ReadUInt32();

            List<GuiGeometryMesh> geometryMeshes = [];

            // GuiGeometryMesh
            for (int j = 0; i < numMeshes; i++)
            {
                reader.BaseStream.Seek(geometryMeshesPtr + (0x4 * i), SeekOrigin.Begin);
                reader.BaseStream.Seek(reader.ReadUInt32(), SeekOrigin.Begin);

                // GuiGeometryMeshHeader
                EMeshType meshType = (EMeshType) reader.ReadInt32();
                ETextureType textureMode = (ETextureType) reader.ReadInt32();
                int textureID = reader.ReadInt32();
                ResourceImport.ReadExternalImport(textureID - 1, reader, ToNext0x10(sizedata), out ResourceImport resourceImport);
                _ = reader.ReadUInt32();

                uint numVerts = reader.ReadUInt32();
                uint vertsPtr = reader.ReadUInt32();

                List<GuiVertex> vertices = [];

                // GuiVertex
                for (int k = 0; i < numVerts; i++)
                {
                    reader.BaseStream.Seek(vertsPtr + (0x4 * i), SeekOrigin.Begin);
                    reader.BaseStream.Seek(reader.ReadUInt32(), SeekOrigin.Begin);

                    vertices.Add(new GuiVertex()
                    {
                        Position = reader.ReadVector2Literal(),
                        Color = reader.ReadColorRGBA8(),
                        UV = reader.ReadVector2Literal()
                    });
                }

                geometryMeshes.Add(new GuiGeometryMesh()
                {
                    MeshType = meshType,
                    TextureMode = textureMode,
                    TextureReference = resourceImport,
                    Vertices = vertices,
                });
            }

            guiGeometryFiles.Add(new GuiGeometryFile()
            {
                ID = muID,
                GeometryMeshes = geometryMeshes
            });
        }

        GuiGeometry = new GuiGeometryObject()
        {
            GeometryFiles = guiGeometryFiles
        };
    }

    public AptData() : base() { }

    public AptData(string path, Endian endianness = Endian.Agnostic) : base(path, endianness) { }
}

public struct GuiGeometryObject
{
    public List<GuiGeometryFile> GeometryFiles;
}

public struct GuiGeometryFile
{
    public uint ID;
    public List<GuiGeometryMesh> GeometryMeshes;
};

public struct GuiGeometryMesh
{
    public EMeshType MeshType;
    public ETextureType TextureMode;
    public ResourceImport TextureReference;
    public List<GuiVertex> Vertices;
}

public struct GuiVertex
{
    public Vector2Literal Position;
    public ColorRGBA8 Color;
    public Vector2Literal UV;
}

public enum EMeshType : int
{
    TriList = 0,
    TriangleStrip = 1,
    LineList = 2
}

public enum ETextureType : int
{
    Vector = 0,
    TexturedClamp = 1,
    TexturedWrap = 2
}