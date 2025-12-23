namespace Volatility.Resources;

public class ShaderBase : Resource
{
    public override ResourceType GetResourceType() => ResourceType.Shader;
    public override Endian GetResourceEndian() => Endian.Agnostic;
    public override Platform GetResourcePlatform() => Platform.Agnostic;

    [EditorCategory("Shader/Source"), EditorLabel("Source Text"), EditorTooltip("The HLSL source text for this shader.")]
    public string? ShaderSourceText { get; set; }

    [EditorCategory("Shader/Compile"), EditorLabel("Stages"), EditorTooltip("Entry points and profiles to compile from this source.")]
    public List<ShaderStageCompile> Stages { get; set; } = [];

    [EditorHidden]
    [EditorCategory("Shader/Compile"), EditorLabel("Entry Point"), EditorTooltip("The entry point function name.")]
    public string EntryPoint { get; set; } = "main";

    [EditorHidden]
    [EditorCategory("Shader/Compile"), EditorLabel("Target Profile"), EditorTooltip("The shader profile (e.g. vs_5_0, ps_5_0).")]
    public string TargetProfile { get; set; } = "ps_5_0";

    [EditorCategory("Shader/Compile"), EditorLabel("Defines"), EditorTooltip("Preprocessor defines for compilation.")]
    public List<ShaderDefine> Defines { get; set; } = [];

    [EditorCategory("Shader/Compile"), EditorLabel("Include Directories"), EditorTooltip("Additional include search paths.")]
    public List<string> IncludeDirectories { get; set; } = [];

    [EditorCategory("Shader/Compile"), EditorLabel("Additional Arguments"), EditorTooltip("Extra dxc command-line arguments.")]
    public List<string> AdditionalArguments { get; set; } = [];

    public override void WriteToStream(EndianAwareBinaryWriter writer, Endian endianness)
    {
        base.WriteToStream(writer, endianness);
    }
    public override void ParseFromStream(ResourceBinaryReader reader, Endian endianness)
    {
        base.ParseFromStream(reader, endianness);
    }

    public IReadOnlyList<ShaderStageCompile> GetCompileStages()
    {
        if (Stages != null && Stages.Count > 0)
        {
            return Stages;
        }

        string entryPoint = string.IsNullOrWhiteSpace(EntryPoint) ? "main" : EntryPoint;
        string targetProfile = string.IsNullOrWhiteSpace(TargetProfile) ? "ps_5_0" : TargetProfile;

        return
        [
            new ShaderStageCompile
            {
                Stage = ShaderStageCompile.GetStageFromProfile(targetProfile),
                EntryPoint = entryPoint,
                TargetProfile = targetProfile
            }
        ];
    }

    public ShaderBase() : base() { }

    public ShaderBase(string path) : base(path) { }

    public ShaderBase(string path, Endian endianness = Endian.Agnostic) : base(path, endianness) { }
}

public enum ShaderStageType
{
    Unknown = 0,
    Vertex,
    Pixel,
    Geometry,
    Hull,
    Domain,
    Compute
}

public sealed class ShaderStageCompile
{
    [EditorLabel("Stage"), EditorCategory("Shader/Compile/Stages"), EditorTooltip("The shader stage type.")]
    public ShaderStageType Stage { get; set; } = ShaderStageType.Pixel;

    [EditorLabel("Entry Point"), EditorCategory("Shader/Compile/Stages"), EditorTooltip("The entry point function name.")]
    public string EntryPoint { get; set; } = "main";

    [EditorLabel("Target Profile"), EditorCategory("Shader/Compile/Stages"), EditorTooltip("The shader profile (e.g. vs_5_0, ps_5_0).")]
    public string TargetProfile { get; set; } = "ps_5_0";

    [EditorLabel("Defines"), EditorCategory("Shader/Compile/Stages"), EditorTooltip("Stage-specific preprocessor defines for compilation.")]
    public List<ShaderDefine> Defines { get; set; } = [];

    [EditorLabel("Additional Arguments"), EditorCategory("Shader/Compile/Stages"), EditorTooltip("Stage-specific extra dxc command-line arguments.")]
    public List<string> AdditionalArguments { get; set; } = [];

    public ShaderStageType ResolveStage()
    {
        return Stage != ShaderStageType.Unknown ? Stage : GetStageFromProfile(TargetProfile);
    }

    public static ShaderStageType GetStageFromProfile(string? targetProfile)
    {
        if (string.IsNullOrWhiteSpace(targetProfile))
            return ShaderStageType.Unknown;

        string profile = targetProfile.Trim().ToLowerInvariant();
        if (profile.StartsWith("vs_"))
            return ShaderStageType.Vertex;
        if (profile.StartsWith("ps_"))
            return ShaderStageType.Pixel;
        if (profile.StartsWith("gs_"))
            return ShaderStageType.Geometry;
        if (profile.StartsWith("hs_"))
            return ShaderStageType.Hull;
        if (profile.StartsWith("ds_"))
            return ShaderStageType.Domain;
        if (profile.StartsWith("cs_"))
            return ShaderStageType.Compute;

        return ShaderStageType.Unknown;
    }

    public static string? GetProfilePrefix(ShaderStageType stage)
    {
        return stage switch
        {
            ShaderStageType.Vertex => "vs",
            ShaderStageType.Pixel => "ps",
            ShaderStageType.Geometry => "gs",
            ShaderStageType.Hull => "hs",
            ShaderStageType.Domain => "ds",
            ShaderStageType.Compute => "cs",
            _ => null
        };
    }

    public static string GetStageSuffix(ShaderStageType stage)
    {
        return stage switch
        {
            ShaderStageType.Vertex => "vs",
            ShaderStageType.Pixel => "ps",
            ShaderStageType.Geometry => "gs",
            ShaderStageType.Hull => "hs",
            ShaderStageType.Domain => "ds",
            ShaderStageType.Compute => "cs",
            _ => "unknown"
        };
    }
}

public sealed class ShaderDefine
{
    [EditorLabel("Name"), EditorCategory("Shader/Compile/Defines"), EditorTooltip("Define name.")]
    public string Name { get; set; } = string.Empty;

    [EditorLabel("Value"), EditorCategory("Shader/Compile/Defines"), EditorTooltip("Optional define value.")]
    public string? Value { get; set; }
}
