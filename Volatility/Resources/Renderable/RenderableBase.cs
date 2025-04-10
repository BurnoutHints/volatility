﻿using System.Collections;

using Volatility.Utilities;

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
    public BitArray Flags = new BitArray(16);
    public uint IndexBuffer;                    // Only on PC platforms
    public uint VertexBuffer;                   // Only on PC platforms

    public override ResourceType GetResourceType() => ResourceType.Renderable;

    public override void ParseFromStream(ResourceBinaryReader reader, Endian endianness = Endian.Agnostic)
    {
        base.ParseFromStream(reader, endianness);

        BoundingSphere[0] = reader.ReadSingle();            // X
        BoundingSphere[1] = reader.ReadSingle();            // Y
        BoundingSphere[2] = reader.ReadSingle();            // Z
        BoundingSphere[3] = reader.ReadSingle();            // Plus
        Version = reader.ReadUInt16();
        NumMeshes = reader.ReadUInt16();
        Meshes = reader.ReadUInt32();                       // Pointer to a pointer
        reader.BaseStream.Seek(0x4, SeekOrigin.Current);    // mpObjectScopeTextureInfo
        using (BitReader bitReader = new BitReader(reader.ReadBytes(4)))
        {
            Flags = bitReader.ReadBitsToBitArray(16);
        }

        // TODO: Parse RenderableMeshes
    }

    public RenderableBase(string path, Endian endianness = Endian.Agnostic) : base(path, endianness) { }
}


