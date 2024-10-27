namespace Volatility.Resource.Texture;

public abstract class TextureHeaderBase : Resource
{
    public override ResourceType GetResourceType() => ResourceType.Texture;

    public ushort Width { get; set; }
    public ushort Height { get; set; }
    public ushort Depth { get; set; }
    public byte MipmapLevels { get; set; }

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
            string folder = "";
            var directoryInfo = new DirectoryInfo(ImportPath);

            // Two directories up
            if (directoryInfo.Parent?.Parent != null)
            {
                folder = directoryInfo.Parent.Parent.Name;

                WorldTexture = folder.StartsWith("TRK_") || folder.Contains("BACKDROP") || folder.Contains("WORLDTEX");
                GRTexture = folder.EndsWith("_GR");
                PropTexture = folder.Contains("PROPS");
            }
        }
    }

    public override void PullAll()
    {
        PullInternalDimension();
        PullInternalFormat();
        PullInternalFlags();
    }
    public override void PushAll()
    {
        PushInternalDimension();
        PushInternalFormat();
        PushInternalFlags();
    }

    public TextureHeaderBase() : base() => Depth = 1;

    public TextureHeaderBase(string path) : base(path) { }
}
// BPR formatted but converted for each platform
public enum DIMENSION : int
{
    DIMENSION_1D = 6,
    DIMENSION_2D = 7,
    DIMENSION_3D = 8,
    DIMENSION_CUBE = 9
}