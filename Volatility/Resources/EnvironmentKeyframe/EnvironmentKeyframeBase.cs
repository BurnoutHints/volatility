namespace Volatility.Resources.EnvironmentKeyframe;

public class EnvironmentKeyframeBase : Resource 
{
    public override ResourceType GetResourceType() => ResourceType.EnvironmentKeyframe;
    
    public BloomData BloomSettings;
    public VignetteData VignetteSettings;
    public TintData TintSettings;
    public ScatteringData ScatteringSettings;
    public LightingData LightingSettings;
    public CloudsData CloudSettings;

    public EnvironmentKeyframeBase() : base() { }

    public EnvironmentKeyframeBase(string path) : base(path) { } 

    public override void ParseFromStream(ResourceBinaryReader reader)
    {
        base.ParseFromStream(reader);

        if (reader.ReadUInt32() != 8)
        {
            throw new Exception("Version mismatch!");
        }

        BloomSettings = new BloomData(reader);
        VignetteSettings = new VignetteData(reader);
        TintSettings = new TintData(reader, GetResourceArch());
        reader.BaseStream.Seek(0xC, SeekOrigin.Current);
        ScatteringSettings = new ScatteringData(reader);
        LightingSettings = new LightingData(reader);
        CloudSettings = new CloudsData(reader);
    }
}

public struct BloomData
{
    public float Luminance;
    public float Threshold;
    public Vector4 Scale;

    public BloomData(ResourceBinaryReader reader) 
    {
        Luminance = reader.ReadSingle();
        Threshold = reader.ReadSingle();
        reader.BaseStream.Seek(0x8, SeekOrigin.Current);
        Scale = reader.ReadVector4();
    }
}

public struct VignetteData 
{
    public float Angle;
    public float Sharpness;
    public Vector2 Amount;
    public Vector2 Center;
    public ColorRGBA InnerColor;
    public ColorRGBA OuterColor;

    public VignetteData(ResourceBinaryReader reader) 
    {
        Angle = reader.ReadSingle();
        Sharpness = reader.ReadSingle();
        reader.BaseStream.Seek(0x8, SeekOrigin.Current);
        Amount = reader.ReadVector2();
        Center = reader.ReadVector2();
        InnerColor = reader.ReadColorRGBA();
        OuterColor = reader.ReadColorRGBA();
    }
}

public struct TintData 
{
    ulong ColorCubePtr; // TODO: Update with new ResourceID system
    public TintData(ResourceBinaryReader reader, Arch arch) 
    {
        ColorCubePtr = (arch == Arch.x64 ? reader.ReadUInt64() : reader.ReadUInt32());
    }
}

public struct ScatteringData 
{
    Vector3 SkyTopColor;
    Vector3 SkyHorizonColor;
    Vector3 SkySunColor;
    float SkyHorizonPower;
    float SkySunPower;
    float SkyDarkening;
    float SkyHorizonBleedScale;
    float SkyHorizonBleedPower;
    float SkySunBleedPower;
    Vector3 ScatteringTopColor;
    Vector3 ScatteringHorizonColor;
    Vector3 ScatteringSunColor;
    float ScatteringHorizonPower;
    float ScatteringSunPower;
    float ScatteringDarkening;
    float ScatteringHorizonBleedScale;
    float ScatteringHorizonBleedPower;
    float ScatteringSunBleedPower;
    Vector2Literal ScatteringDistance;
    float ScatteringPower;
    float ScatteringCap;

    public ScatteringData(ResourceBinaryReader reader) 
    {
        SkyTopColor = reader.ReadVector3();
        SkyHorizonColor = reader.ReadVector3();
        SkySunColor = reader.ReadVector3();
        SkyHorizonPower = reader.ReadSingle();
        SkySunPower = reader.ReadSingle();
        SkyDarkening = reader.ReadSingle();
        SkyHorizonBleedScale = reader.ReadSingle();
        SkyHorizonBleedPower = reader.ReadSingle();
        SkySunBleedPower = reader.ReadSingle();
        reader.BaseStream.Seek(0x8, SeekOrigin.Current);
        ScatteringTopColor = reader.ReadVector3();
        ScatteringHorizonColor = reader.ReadVector3();
        ScatteringSunColor = reader.ReadVector3();
        ScatteringHorizonPower = reader.ReadSingle();
        ScatteringSunPower = reader.ReadSingle();
        ScatteringDarkening = reader.ReadSingle();
        ScatteringHorizonBleedScale = reader.ReadSingle();
        ScatteringHorizonBleedPower = reader.ReadSingle();
        ScatteringSunBleedPower = reader.ReadSingle();
        ScatteringDistance = reader.ReadVector2Literal();
        ScatteringPower = reader.ReadSingle();
        ScatteringCap = reader.ReadSingle();
        reader.BaseStream.Seek(0x8, SeekOrigin.Current);
    }
}

public struct LightingData 
{
    Vector3	KeyLightColor;
    Vector3	SpecularColor;
    Vector3	KeyFillColor;
    Vector3	ShadowFillColor;
    Vector3	RightFillColor;
    Vector3	LeftFillColor;
    Vector3	UpFillColor;
    Vector3	DownFillColor;
    float AmbientIrradianceScale;

    public LightingData(ResourceBinaryReader reader) 
    {
        KeyLightColor = reader.ReadVector3();
        SpecularColor = reader.ReadVector3();
        KeyFillColor = reader.ReadVector3();
        ShadowFillColor = reader.ReadVector3();
        RightFillColor = reader.ReadVector3();
        LeftFillColor = reader.ReadVector3();
        UpFillColor = reader.ReadVector3();
        DownFillColor = reader.ReadVector3();
        AmbientIrradianceScale = reader.ReadSingle();
        reader.BaseStream.Seek(0xC, SeekOrigin.Current);
    }
}

public struct CloudsData 
{
    ColorRGB Layer1LiteColor;
    ColorRGB Layer1DarkColor;
    ColorRGB Layer2LiteColor;
    ColorRGB Layer2DarkColor;
    Vector2Literal LayerDensity;
    Vector2Literal LayerFeathering;
    Vector2Literal LayerOpacity;
    Vector2Literal LayerSpeed;
    Vector2Literal LayerScale;
    float DirectionAngle;

    public CloudsData(ResourceBinaryReader reader) 
    {
        Layer1LiteColor = reader.ReadColorRGB();
        Layer2LiteColor = reader.ReadColorRGB();
        Layer1DarkColor = reader.ReadColorRGB();
        Layer2DarkColor = reader.ReadColorRGB();
        LayerDensity = reader.ReadVector2Literal();
        LayerFeathering = reader.ReadVector2Literal();
        LayerOpacity = reader.ReadVector2Literal();
        LayerSpeed = reader.ReadVector2Literal();
        LayerScale = reader.ReadVector2Literal();
        DirectionAngle = reader.ReadSingle();
        reader.BaseStream.Seek(0x4, SeekOrigin.Current);
    }
}