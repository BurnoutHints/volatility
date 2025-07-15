using System.Collections;

using Volatility.Utilities;
using Volatility.Extensions;

namespace Volatility.Resources;

// The Renderable resource type contains all the 3D data used by each
// Model resource in Burnout Paradise. Essentially, Renderables hold the
// geometric and visual information needed for rendering models in-game.

// Learn More:
// https://burnout.wiki/wiki/Renderable

public abstract class RenderableBase : Resource
{
    public Vector3Plus BoundingSphere;
    public ushort Version;
    public ushort NumMeshes;
    public uint Meshes;                         // TODO
    public RenderableFlags Flags;
    public ulong IndexBuffer;                    // Only on PC platforms
    public ulong VertexBuffer;                   // Only on PC platforms

    public override ResourceType GetResourceType() => ResourceType.Renderable;

    public override void ParseFromStream(BinaryReader reader, Endian n = Endian.Agnostic)
    {
        base.ParseFromStream(reader, n);
        
        BoundingSphere = reader.ReadVector3Plus(n);
        Version = reader.ReadUInt16(n);
        NumMeshes = reader.ReadUInt16(n);
        if (GetResourceArch() == Arch.x64) _ = reader.ReadUInt32(n);
        Meshes = reader.ReadUInt32(n);                      // Pointer to a pointer
        _ = reader.ReadPointer(GetResourceArch(), n);       // mpObjectScopeTextureInfo
        Flags = reader.ReadEnum<RenderableFlags>(n);

        // TODO: Parse RenderableMeshes
    }

    public RenderableBase(string path, Endian endianness = Endian.Agnostic) : base(path, endianness) { }
    
    [Flags]
    public enum RenderableFlags : ushort
    {
        None      = 0x0,
        Flag1     = 0x1,
        Flag2     = 0x2,
        Flag4     = 0x4,
        Flag8     = 0x8,
        Flag16    = 0x10,
        Flag32    = 0x20,
        Flag64    = 0x40,
        Flag128   = 0x80,
        Flag256   = 0x100,
        Flag512   = 0x200,
        Flag1024  = 0x400,
        Flag2048  = 0x800,
    }
}


