namespace Volatility.Resources;

// The Environment Keyframe resource is the primary way to control the game's 
// environment at a single point within the Environment's timeline, with settings
// including bloom, vignette, tint (color grading), scattering, and lighting.

// Learn More:
// https://burnout.wiki/wiki/Environment_Keyframe

public class EnvironmentKeyframe : Resource 
{
    public override ResourceType GetResourceType() => ResourceType.EnvironmentKeyframe;
    
    public BloomData BloomSettings;
    public VignetteData VignetteSettings;
    public TintData TintSettings;
    public ScatteringData ScatteringSettings;
    public LightingData LightingSettings;
    public CloudsData CloudSettings;

    public EnvironmentKeyframe() : base() { }

    public EnvironmentKeyframe(string path, Endian endianness = Endian.Agnostic) : base(path, endianness) { } 

    public override void ParseFromStream(ResourceBinaryReader reader, Endian endianness = Endian.Agnostic)
    {
        base.ParseFromStream(reader, endianness);

        if (reader.ReadUInt32() != 8)
        {
            throw new Exception("Version mismatch!");
        }

        reader.BaseStream.Seek(0x10, SeekOrigin.Begin);

        BloomSettings = new BloomData(reader);
        VignetteSettings = new VignetteData(reader);
        TintSettings = new TintData(reader, GetResourceArch());
        reader.BaseStream.Seek(0xC, SeekOrigin.Current);
        ScatteringSettings = new ScatteringData(reader);
        LightingSettings = new LightingData(reader);
        CloudSettings = new CloudsData(reader);
    }

    public override void WriteToStream(EndianAwareBinaryWriter writer, Endian endianness = Endian.Agnostic)
    {
        base.WriteToStream(writer, endianness);

        writer.Write((uint)0x8);
        writer.Write(new byte[0xC]);

        writer.Write(BloomSettings.Luminance);
        writer.Write(BloomSettings.Threshold);
        writer.Write(new byte[0x8]);
        writer.Write(BloomSettings.Scale);

        writer.Write(VignetteSettings.Angle);
        writer.Write(VignetteSettings.Sharpness);
        writer.Write(new byte[0x8]);
        writer.Write(VignetteSettings.Amount, intrinsic: true);
        writer.Write(VignetteSettings.Center, intrinsic: true);
        writer.Write(VignetteSettings.InnerColor);
        writer.Write(VignetteSettings.OuterColor);

        writer.Write((uint)0x0); // TODO: handle external ColourCube import
        writer.Write(new byte[0xC]);

        writer.Write(ScatteringSettings.SkyTopColor, intrinsic: true);
        writer.Write(ScatteringSettings.SkyHorizonColor, intrinsic: true);
        writer.Write(ScatteringSettings.SkySunColor, intrinsic: true);
        writer.Write(ScatteringSettings.SkyHorizonPower);
        writer.Write(ScatteringSettings.SkySunPower);
        writer.Write(ScatteringSettings.SkyDarkening);
        writer.Write(ScatteringSettings.SkyHorizonBleedScale);
        writer.Write(ScatteringSettings.SkyHorizonBleedPower);
        writer.Write(ScatteringSettings.SkySunBleedPower);
        writer.Write(new byte[0x8]);
        writer.Write(ScatteringSettings.ScatteringTopColor, intrinsic: true);
        writer.Write(ScatteringSettings.ScatteringHorizonColor, intrinsic: true);
        writer.Write(ScatteringSettings.ScatteringSunColor, intrinsic: true);
        writer.Write(ScatteringSettings.ScatteringHorizonPower);
        writer.Write(ScatteringSettings.ScatteringSunPower);
        writer.Write(ScatteringSettings.ScatteringDarkening);
        writer.Write(ScatteringSettings.ScatteringHorizonBleedScale);
        writer.Write(ScatteringSettings.ScatteringHorizonBleedPower);
        writer.Write(ScatteringSettings.ScatteringSunBleedPower);
        writer.Write(ScatteringSettings.ScatteringDistance, intrinsic: false);
        writer.Write(ScatteringSettings.ScatteringPower);
        writer.Write(ScatteringSettings.ScatteringCap);
        writer.Write(new byte[0x8]);

        writer.Write(LightingSettings.KeyLightColor, intrinsic: true);
        writer.Write(LightingSettings.SpecularColor, intrinsic: true);
        writer.Write(LightingSettings.KeyFillColor, intrinsic: true);
        writer.Write(LightingSettings.ShadowFillColor, intrinsic: true);
        writer.Write(LightingSettings.RightFillColor, intrinsic: true);
        writer.Write(LightingSettings.LeftFillColor, intrinsic: true);
        writer.Write(LightingSettings.UpFillColor, intrinsic: true);
        writer.Write(LightingSettings.DownFillColor, intrinsic: true);
        writer.Write(LightingSettings.AmbientIrradianceScale);
        writer.Write(new byte[0xC]);

        writer.Write(CloudSettings.Layer1LiteColor, intrinsic: true);
        writer.Write(CloudSettings.Layer2LiteColor, intrinsic: true);
        writer.Write(CloudSettings.Layer1DarkColor, intrinsic: true);
        writer.Write(CloudSettings.Layer2DarkColor, intrinsic: true);
        writer.Write(CloudSettings.LayerDensity, intrinsic: false);
        writer.Write(CloudSettings.LayerFeathering, intrinsic: false);
        writer.Write(CloudSettings.LayerOpacity, intrinsic: false);
        writer.Write(CloudSettings.LayerSpeed, intrinsic: false);
        writer.Write(CloudSettings.LayerScale, intrinsic: false);
        writer.Write(CloudSettings.DirectionAngle);
        writer.Write(new byte[0x4]);
    }
}

public struct BloomData
{
    [EditorLabel("Luminance"), EditorCategory("Environment Keyframe/Bloom"), EditorTooltip("The brightness of the bloom effect.")]
    public float Luminance;

    [EditorLabel("Threshold"), EditorCategory("Environment Keyframe/Bloom"), EditorTooltip("The threshold for the bloom effect.")]
    public float Threshold;

    [EditorLabel("Scale"), EditorCategory("Environment Keyframe/Bloom"), EditorTooltip("The scale of the bloom effect.")]
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
    public ulong ColorCubePtr; // TODO: Update with new ResourceID system
    public TintData(ResourceBinaryReader reader, Arch arch) 
    {
        ColorCubePtr = (arch == Arch.x64 ? reader.ReadUInt64() : reader.ReadUInt32());
    }
}

public struct ScatteringData 
{
    public Vector3 SkyTopColor;
    public Vector3 SkyHorizonColor;
    public Vector3 SkySunColor;
    public float SkyHorizonPower;
    public float SkySunPower;
    public float SkyDarkening;
    public float SkyHorizonBleedScale;
    public float SkyHorizonBleedPower;
    public float SkySunBleedPower;
    public Vector3 ScatteringTopColor;
    public Vector3 ScatteringHorizonColor;
    public Vector3 ScatteringSunColor;
    public float ScatteringHorizonPower;
    public float ScatteringSunPower;
    public float ScatteringDarkening;
    public float ScatteringHorizonBleedScale;
    public float ScatteringHorizonBleedPower;
    public float ScatteringSunBleedPower;
    public Vector2Literal ScatteringDistance;
    public float ScatteringPower;
    public float ScatteringCap;

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
    public Vector3 KeyLightColor;
    public Vector3 SpecularColor;
    public Vector3 KeyFillColor;
    public Vector3 ShadowFillColor;
    public Vector3 RightFillColor;
    public Vector3 LeftFillColor;
    public Vector3 UpFillColor;
    public Vector3 DownFillColor;
    public float AmbientIrradianceScale;

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
    public ColorRGB Layer1LiteColor;
    public ColorRGB Layer1DarkColor;
    public ColorRGB Layer2LiteColor;
    public ColorRGB Layer2DarkColor;
    public Vector2Literal LayerDensity;
    public Vector2Literal LayerFeathering;
    public Vector2Literal LayerOpacity;
    public Vector2Literal LayerSpeed;
    public Vector2Literal LayerScale;
    public float DirectionAngle;

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