using System.Runtime.CompilerServices;
using Volatility.Extensions;

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

    public override void ParseFromStream(BinaryReader reader, Endian n = Endian.Agnostic)
    {
        base.ParseFromStream(reader, n);

        if (reader.ReadUInt32() != 8)
        {
            throw new Exception("Version mismatch!");
        }

        reader.BaseStream.Seek(0x10, SeekOrigin.Begin);

        BloomSettings = new BloomData(reader, n);
        VignetteSettings = new VignetteData(reader, n);
        TintSettings = new TintData(reader, n, GetResourceArch());
        reader.BaseStream.Seek(0xC, SeekOrigin.Current);
        ScatteringSettings = new ScatteringData(reader, n);
        LightingSettings = new LightingData(reader, n);
        CloudSettings = new CloudsData(reader, n);
    }

    public override void WriteToStream(BinaryWriter writer, Endian n = Endian.Agnostic)
    {
        base.WriteToStream(writer, n);

        writer.Write(8);
        writer.Write(new byte[0xC]);

        writer.Write(BloomSettings.Luminance, n);
        writer.Write(BloomSettings.Threshold, n);
        writer.Write(new byte[0x8]);
        writer.Write(BloomSettings.Scale, n);

        writer.Write(VignetteSettings.Angle, n);
        writer.Write(VignetteSettings.Sharpness, n);
        writer.Write(new byte[0x8]);
        writer.Write(VignetteSettings.Amount, n, intrinsic: true);
        writer.Write(VignetteSettings.Center, n, intrinsic: true);
        writer.Write(VignetteSettings.InnerColor, n);
        writer.Write(VignetteSettings.OuterColor, n);

        writer.WritePointer(GetResourceArch(), 0x0, n); // TODO: handle external ColourCube import
        writer.Write(new byte[0x10 - GetResourceArch().PointerSize()]);

        writer.Write(ScatteringSettings.SkyTopColor, n, intrinsic: true);
        writer.Write(ScatteringSettings.SkyHorizonColor, n, intrinsic: true);
        writer.Write(ScatteringSettings.SkySunColor, n, intrinsic: true);
        writer.Write(ScatteringSettings.SkyHorizonPower, n);
        writer.Write(ScatteringSettings.SkySunPower, n);
        writer.Write(ScatteringSettings.SkyDarkening, n);
        writer.Write(ScatteringSettings.SkyHorizonBleedScale, n);
        writer.Write(ScatteringSettings.SkyHorizonBleedPower, n);
        writer.Write(ScatteringSettings.SkySunBleedPower, n);
        writer.Write(new byte[0x8]);
        writer.Write(ScatteringSettings.ScatteringTopColor, n, intrinsic: true);
        writer.Write(ScatteringSettings.ScatteringHorizonColor, n, intrinsic: true);
        writer.Write(ScatteringSettings.ScatteringSunColor, n, intrinsic: true);
        writer.Write(ScatteringSettings.ScatteringHorizonPower, n);
        writer.Write(ScatteringSettings.ScatteringSunPower, n);
        writer.Write(ScatteringSettings.ScatteringDarkening, n);
        writer.Write(ScatteringSettings.ScatteringHorizonBleedScale, n);
        writer.Write(ScatteringSettings.ScatteringHorizonBleedPower, n);
        writer.Write(ScatteringSettings.ScatteringSunBleedPower, n);
        writer.Write(ScatteringSettings.ScatteringDistance, n, intrinsic: false);
        writer.Write(ScatteringSettings.ScatteringPower, n);
        writer.Write(ScatteringSettings.ScatteringCap, n);
        writer.Write(new byte[0x8]);

        writer.Write(LightingSettings.KeyLightColor, n, intrinsic: true);
        writer.Write(LightingSettings.SpecularColor, n, intrinsic: true);
        writer.Write(LightingSettings.KeyFillColor, n, intrinsic: true);
        writer.Write(LightingSettings.ShadowFillColor, n, intrinsic: true);
        writer.Write(LightingSettings.RightFillColor, n, intrinsic: true);
        writer.Write(LightingSettings.LeftFillColor, n, intrinsic: true);
        writer.Write(LightingSettings.UpFillColor, n, intrinsic: true);
        writer.Write(LightingSettings.DownFillColor, n, intrinsic: true);
        writer.Write(LightingSettings.AmbientIrradianceScale, n);
        writer.Write(new byte[0xC]);

        writer.Write(CloudSettings.Layer1LiteColor, n, intrinsic: true);
        writer.Write(CloudSettings.Layer2LiteColor, n, intrinsic: true);
        writer.Write(CloudSettings.Layer1DarkColor, n, intrinsic: true);
        writer.Write(CloudSettings.Layer2DarkColor, n, intrinsic: true);
        writer.Write(CloudSettings.LayerDensity, n, intrinsic: false);
        writer.Write(CloudSettings.LayerFeathering, n, intrinsic: false);
        writer.Write(CloudSettings.LayerOpacity, n, intrinsic: false);
        writer.Write(CloudSettings.LayerSpeed, n, intrinsic: false);
        writer.Write(CloudSettings.LayerScale, n, intrinsic: false);
        writer.Write(CloudSettings.DirectionAngle, n);
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

    public BloomData(BinaryReader reader, Endian n) 
    {
        Luminance = reader.ReadSingle(n);
        Threshold = reader.ReadSingle(n);
        reader.BaseStream.Seek(0x8, SeekOrigin.Current);
        Scale = reader.ReadVector4(n);
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

    public VignetteData(BinaryReader reader, Endian n) 
    {
        Angle = reader.ReadSingle(n);
        Sharpness = reader.ReadSingle(n);
        reader.BaseStream.Seek(0x8, SeekOrigin.Current);
        Amount = reader.ReadVector2(n);
        Center = reader.ReadVector2(n);
        InnerColor = reader.ReadColorRGBA(n);
        OuterColor = reader.ReadColorRGBA(n);
    }
}

public struct TintData
{
    public ResourceImport ColorCubeReference;
    public TintData(BinaryReader reader, Endian n, Arch arch) 
    {
        ColorCubeReference.ReferenceID = reader.ReadPointer(arch, n);
        
        if (ResourceImport.ReadExternalImport(0, reader, n, 0x240, out ResourceImport ExternalReference))
            ColorCubeReference = ExternalReference;
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

    public ScatteringData(BinaryReader reader, Endian n) 
    {
        SkyTopColor = reader.ReadVector3(n);
        SkyHorizonColor = reader.ReadVector3(n);
        SkySunColor = reader.ReadVector3(n);
        SkyHorizonPower = reader.ReadSingle(n);
        SkySunPower = reader.ReadSingle(n);
        SkyDarkening = reader.ReadSingle(n);
        SkyHorizonBleedScale = reader.ReadSingle(n);
        SkyHorizonBleedPower = reader.ReadSingle(n);
        SkySunBleedPower = reader.ReadSingle(n);
        reader.BaseStream.Seek(0x8, SeekOrigin.Current);
        ScatteringTopColor = reader.ReadVector3(n);
        ScatteringHorizonColor = reader.ReadVector3(n);
        ScatteringSunColor = reader.ReadVector3(n);
        ScatteringHorizonPower = reader.ReadSingle(n);
        ScatteringSunPower = reader.ReadSingle(n);
        ScatteringDarkening = reader.ReadSingle(n);
        ScatteringHorizonBleedScale = reader.ReadSingle(n);
        ScatteringHorizonBleedPower = reader.ReadSingle(n);
        ScatteringSunBleedPower = reader.ReadSingle(n);
        ScatteringDistance = reader.ReadVector2Literal(n);
        ScatteringPower = reader.ReadSingle(n);
        ScatteringCap = reader.ReadSingle(n);
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

    public LightingData(BinaryReader reader, Endian n) 
    {
        KeyLightColor = reader.ReadVector3(n);
        SpecularColor = reader.ReadVector3(n);
        KeyFillColor = reader.ReadVector3(n);
        ShadowFillColor = reader.ReadVector3(n);
        RightFillColor = reader.ReadVector3(n);
        LeftFillColor = reader.ReadVector3(n);
        UpFillColor = reader.ReadVector3(n);
        DownFillColor = reader.ReadVector3(n);
        AmbientIrradianceScale = reader.ReadSingle(n);
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

    public CloudsData(BinaryReader reader, Endian n) 
    {
        Layer1LiteColor = reader.ReadColorRGB(n);
        Layer2LiteColor = reader.ReadColorRGB(n);
        Layer1DarkColor = reader.ReadColorRGB(n);
        Layer2DarkColor = reader.ReadColorRGB(n);
        LayerDensity = reader.ReadVector2Literal(n);
        LayerFeathering = reader.ReadVector2Literal(n);
        LayerOpacity = reader.ReadVector2Literal(n);
        LayerSpeed = reader.ReadVector2Literal(n);
        LayerScale = reader.ReadVector2Literal(n);
        DirectionAngle = reader.ReadSingle(n);
        reader.BaseStream.Seek(0x4, SeekOrigin.Current);
    }
}