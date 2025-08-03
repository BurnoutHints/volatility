namespace Volatility.Resources;

// The Texture resource type contains in-game images, which are either displayed
// through Apt UI, applied to models, or used as cubemaps. Textures vary by platform,
// but generally use codecs like DXT1, DXT5, and A8R8G8B8. Mipmaps are typically used
// systematically throughout the game.

// Learn More:
// https://burnout.wiki/wiki/Texture

public abstract class TextureBase : Resource
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

    // TODO: Figure out a solution for platforms that don't support most detailed mipmap (TUB PC & PS3).
    // Trimming to the most detailed mip is a possible solution, but is destructive if the user ever wanted to port the exported texture to another platform.
    [EditorCategory("Texture"), EditorLabel("Most Detailed Mipmap"), EditorTooltip("The index of the most detailed mipmap. Note that index 0 is the highest resolution (the base image).")]
    public byte MostDetailedMip { get; set; }

    protected TextureBaseUsageFlags _usageFlags = TextureBaseUsageFlags.None;
    public TextureBaseUsageFlags UsageFlags
    {
        get => _usageFlags;
        set
        {
            _usageFlags = value;
            PushInternalFlags();
        }
    }
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
        if (string.IsNullOrEmpty(ImportedFileName)
            || new DirectoryInfo(ImportedFileName).Parent?.Parent is not { } gp)
            return;

        string folder = gp.Name;

        const StringComparison OIC = StringComparison.OrdinalIgnoreCase;

        TextureBaseUsageFlags add = TextureBaseUsageFlags.None;

        if (folder.StartsWith("TRK_", OIC) || folder.Contains("BACKDROP", OIC) || folder.Contains("WORLDTEX", OIC))
            add |= TextureBaseUsageFlags.WorldTexture;

        if (folder.EndsWith("_GR", OIC))
            add |= TextureBaseUsageFlags.GRTexture;

        if (folder.Contains("PROPS", OIC))
            add |= TextureBaseUsageFlags.PropTexture;

        // We only set UsageFlags property once to avoid updating pushing three times
        UsageFlags = (UsageFlags & ~TextureBaseUsageFlags.AnyTexture) | add;
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

    public TextureBase() : base() => Depth = 1;

    public TextureBase(string path, Endian endianness = Endian.Agnostic) : base(path, endianness) { }
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

[Flags]
public enum TextureBaseUsageFlags
{
    None = 0,

    GRTexture = 1,      // Vehicle & Wheel GRs 
    WorldTexture = 2,   // GlobalBackdrops & WorldTex
    PropTexture = 3,    // GlobalProps

    AnyTexture = WorldTexture | GRTexture | PropTexture
}