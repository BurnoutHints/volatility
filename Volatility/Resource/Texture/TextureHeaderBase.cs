
namespace Volatility.Resources.Texture;

public abstract class TextureHeaderBase : Resource
{
    public override ResourceType GetResourceType() => ResourceType.Texture;

    [EditorCategory("Texture"), EditorLabel("Width"), EditorTooltip("The target width of the texture.")]
    public ushort Width { get; set; }

    [EditorCategory("Texture"), EditorLabel("Height"), EditorTooltip("The target height of the texture.")]
    public ushort Height { get; set; }

    [EditorCategory("Texture"), EditorLabel("Depth"), EditorTooltip("The depth of the texture. This only applies to volume textures.")]
    public ushort Depth { get; set; }

    [EditorCategory("Texture"), EditorLabel("Amount of Mipmaps"), EditorTooltip("The amount of mipmaps present in the texture. Note that there will always be at least one (the base image).")]
    public byte MipmapLevels { get; set; }

    [EditorHidden]
    protected bool _GRTexture = false;
    
    [EditorCategory("Texture"), EditorLabel("Used in 3D Space"), EditorTooltip("Whether the texture is used in 3D space (vehicles, world).")]
    public bool GRTexture       // Vehicle & Wheel GRs 
    {
        get => _GRTexture;
        set
        {
            _GRTexture = value;
            PushInternalFlags();
        }
    }
    
    [EditorHidden]
    protected bool _WorldTexture = false;
    
    [EditorCategory("Texture"), EditorLabel("Used on World"), EditorTooltip("Whether the texture is used specifically on world assets.")]
    public bool WorldTexture    // GlobalBackdrops & WorldTex
    {
        get => _WorldTexture;
        set
        {
            _WorldTexture = value;
            PushInternalFlags();
        }
    }
    
    [EditorHidden]
    protected bool _PropTexture = false;
    
    [EditorCategory("Texture"), EditorLabel("Used on Props"), EditorTooltip("Whether the texture is used specifically on props.")]
    public bool PropTexture     // GlobalProps
    {
        get => _PropTexture;
        set
        {
            _PropTexture = value;
            PushInternalFlags();
        }
    }
    
    [EditorHidden]
    protected DIMENSION _Dimension = DIMENSION.DIMENSION_2D;
   
    [EditorCategory("Texture"), EditorLabel("Dimension"), EditorTooltip("Determines the depth/dimension of the texture itself (1D, 2D, 3D).")]
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

        if (!string.IsNullOrEmpty(ImportedFileName))
        {
            string folder = "";
            var directoryInfo = new DirectoryInfo(ImportedFileName);

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
    [EditorLabel("1D")]
    DIMENSION_1D = 6,
    [EditorLabel("2D")]
    DIMENSION_2D = 7,
    [EditorLabel("3D")]
    DIMENSION_3D = 8,
    [EditorLabel("Cube")]
    DIMENSION_CUBE = 9
}