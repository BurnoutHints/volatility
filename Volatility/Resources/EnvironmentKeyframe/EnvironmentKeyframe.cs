namespace Volatility.Resources;

// The Environment Keyframe resource is the primary way to control the game's 
// environment at a single point within the Environment's timeline, with settings
// including bloom, vignette, tint (color grading), scattering, and lighting.

// Learn More:
// https://burnout.wiki/wiki/Environment_Keyframe

public class EnvironmentKeyframe : Resource 
{
    public override ResourceType ResourceType => ResourceType.EnvironmentKeyframe;
    
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

        reader.ParseSection(0x10, r => new BloomData(r), out BloomSettings);
        reader.ParseSection(0x30, r => new VignetteData(r), out VignetteSettings);
        reader.ParseSection(0x80, r => new TintData(r, ResourceArch), out TintSettings);
        reader.ParseSection(0x90, r => new ScatteringData(r), out ScatteringSettings);
        reader.ParseSection(0x140, r => new LightingData(r), out LightingSettings);
        reader.ParseSection(0x1D0, r => new CloudsData(r), out CloudSettings);
    }

    public override void WriteToStream(ResourceBinaryWriter writer, Endian endianness = Endian.Agnostic)
    {
        base.WriteToStream(writer, endianness);

        writer.Write((uint)0x8);
        writer.Write(new byte[0xC]);

        writer.WriteSection(writer.BaseStream.Position, BloomSettings, BloomData.Write);
        writer.WriteSection(writer.BaseStream.Position, VignetteSettings, VignetteData.Write);
        writer.WriteSection(writer.BaseStream.Position, TintSettings, TintData.Write);
        writer.WriteSection(writer.BaseStream.Position, ScatteringSettings, ScatteringData.Write);
        writer.WriteSection(writer.BaseStream.Position, LightingSettings, LightingData.Write);
        writer.WriteSection(writer.BaseStream.Position, CloudSettings, CloudsData.Write);
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

    public static void Write(ResourceBinaryWriter writer, BloomData value)
    {
        writer.Write(value.Luminance);
        writer.Write(value.Threshold);
        writer.Write(new byte[0x8]);
        writer.Write(value.Scale);
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

    public static void Write(ResourceBinaryWriter writer, VignetteData value)
    {
        writer.Write(value.Angle);
        writer.Write(value.Sharpness);
        writer.Write(new byte[0x8]);
        writer.Write(value.Amount, intrinsic: true);
        writer.Write(value.Center, intrinsic: true);
        writer.Write(value.InnerColor);
        writer.Write(value.OuterColor);
    }
}

public struct TintData 
{
    public ResourceImport ColorCubeReference;
    public TintData(ResourceBinaryReader reader, Arch arch) 
    {
        ColorCubeReference.ReferenceID = (arch == Arch.x64 ? reader.ReadUInt64() : reader.ReadUInt32());
        
        if (ResourceImport.ReadExternalImport(0, reader, 0x240, out ResourceImport ExternalReference))
            ColorCubeReference = ExternalReference;
    }

    public static void Write(ResourceBinaryWriter writer, TintData value)
    {
        writer.Write((uint)0x0); // TODO: handle external ColourCube import
        writer.Write(new byte[0xC]);
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

    public static void Write(ResourceBinaryWriter writer, ScatteringData value)
    {
        writer.Write(value.SkyTopColor, intrinsic: true);
        writer.Write(value.SkyHorizonColor, intrinsic: true);
        writer.Write(value.SkySunColor, intrinsic: true);
        writer.Write(value.SkyHorizonPower);
        writer.Write(value.SkySunPower);
        writer.Write(value.SkyDarkening);
        writer.Write(value.SkyHorizonBleedScale);
        writer.Write(value.SkyHorizonBleedPower);
        writer.Write(value.SkySunBleedPower);
        writer.Write(new byte[0x8]);
        writer.Write(value.ScatteringTopColor, intrinsic: true);
        writer.Write(value.ScatteringHorizonColor, intrinsic: true);
        writer.Write(value.ScatteringSunColor, intrinsic: true);
        writer.Write(value.ScatteringHorizonPower);
        writer.Write(value.ScatteringSunPower);
        writer.Write(value.ScatteringDarkening);
        writer.Write(value.ScatteringHorizonBleedScale);
        writer.Write(value.ScatteringHorizonBleedPower);
        writer.Write(value.ScatteringSunBleedPower);
        writer.Write(value.ScatteringDistance, intrinsic: false);
        writer.Write(value.ScatteringPower);
        writer.Write(value.ScatteringCap);
        writer.Write(new byte[0x8]);
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
    public static void Write(ResourceBinaryWriter writer, LightingData value)
    {
        writer.Write(value.KeyLightColor, intrinsic: true);
        writer.Write(value.SpecularColor, intrinsic: true);
        writer.Write(value.KeyFillColor, intrinsic: true);
        writer.Write(value.ShadowFillColor, intrinsic: true);
        writer.Write(value.RightFillColor, intrinsic: true);
        writer.Write(value.LeftFillColor, intrinsic: true);
        writer.Write(value.UpFillColor, intrinsic: true);
        writer.Write(value.DownFillColor, intrinsic: true);
        writer.Write(value.AmbientIrradianceScale);
        writer.Write(new byte[0xC]);
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
    public static void Write(ResourceBinaryWriter writer, CloudsData value)
    {
        writer.Write(value.Layer1LiteColor, intrinsic: true);
        writer.Write(value.Layer2LiteColor, intrinsic: true);
        writer.Write(value.Layer1DarkColor, intrinsic: true);
        writer.Write(value.Layer2DarkColor, intrinsic: true);
        writer.Write(value.LayerDensity, intrinsic: false);
        writer.Write(value.LayerFeathering, intrinsic: false);
        writer.Write(value.LayerOpacity, intrinsic: false);
        writer.Write(value.LayerSpeed, intrinsic: false);
        writer.Write(value.LayerScale, intrinsic: false);
        writer.Write(value.DirectionAngle);
        writer.Write(new byte[0x4]);
    }
}