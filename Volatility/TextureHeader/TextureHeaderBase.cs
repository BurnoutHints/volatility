﻿namespace Volatility.TextureHeader;

public abstract class TextureHeaderBase
{
    public ushort Width { get; set; }
    public ushort Height { get; set; }
    public ushort Depth { get; set; }
    public byte MipmapLevels { get; set; }

    public string ImportPath;

    protected bool _GRTexture = false;
    public bool GRTexture       // Vehicle & Wheel GRs 
    {
        get => _GRTexture;
        set 
        {
            _GRTexture = value;
            PushInternalFlags(); 
        }
    }
    protected bool _WorldTexture = false;
    public bool WorldTexture    // GlobalBackdrops & WorldTex
    {
        get => _WorldTexture;
        set 
        {
            _WorldTexture = value;
            PushInternalFlags(); 
        }
    }
    protected bool _PropTexture = false;
    public bool PropTexture     // GlobalProps
    {
        get => _PropTexture;
        set 
        {
            _PropTexture = value;
            PushInternalFlags(); 
        }
    }
    protected DIMENSION _Dimension = DIMENSION.DIMENSION_2D;
    public virtual DIMENSION Dimension // GPUDIMENSION / renderengine::Texture::Type
    {
        get => _Dimension;
        set
        {
            _Dimension = value;
            PushInternalDimension();
        }
    }
    public abstract void PushInternalDimension();
    public abstract void PullInternalDimension();
    public abstract void PushInternalFormat();
    public abstract void PullInternalFormat();
    public abstract void PushInternalFlags();
    public virtual void PullInternalFlags()
    {
        if (!string.IsNullOrEmpty(ImportPath))
        {
            // Two directories up
            string Folder = new DirectoryInfo(path: ImportPath).Parent.Parent.Name;
            WorldTexture = Folder.StartsWith("TRK_") || Folder.Contains("BACKDROP") || Folder.Contains("WORLDTEX");
            GRTexture = Folder.EndsWith("_GR");
            PropTexture = Folder.Contains("PROPS");
        }
    }

    public virtual void PullAll()
    {
        PullInternalDimension();
        PullInternalFormat();
        PullInternalFlags();
    }    
    public virtual void PushAll()
    {
        PushInternalDimension();
        PushInternalFormat();
        PushInternalFlags();
    }
    public abstract void WriteToStream(BinaryWriter writer);
    public abstract void ParseFromStream(BinaryReader reader);
    public TextureHeaderBase() 
    {
        Depth = 1;
    }
    
    public TextureHeaderBase(string path)
    {
        ImportPath = path;
        using (BinaryReader reader = new BinaryReader(new FileStream($"{path}", FileMode.Open)))
        {
            ParseFromStream(reader);
        }
    }
}
// BPR formatted but converted for each platform
public enum DIMENSION : int
{
    DIMENSION_1D = 6,
    DIMENSION_2D = 7,
    DIMENSION_3D = 8,
    DIMENSION_CUBE = 9
}