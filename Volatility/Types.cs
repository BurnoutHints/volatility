// Temporary Types
global using ModelPtr = System.IntPtr;

// Permenant Types
global using Vector2 = System.Numerics.Vector2;         // VectorIntrinsic
global using Vector3 = System.Numerics.Vector3;         // VectorIntrinsic
global using Vector3Plus = System.Numerics.Vector4;     // VectorIntrinsic
global using Vector4 = System.Numerics.Vector4;
global using Quaternion = System.Numerics.Quaternion;
global using Matrix44Affine = System.Numerics.Matrix4x4;

// Volatilty Types
global using ColorRGB = System.Numerics.Vector3;
global using ColorRGBA = System.Numerics.Vector4;
global using Vector2Literal = System.Numerics.Vector2;
using System.Runtime.InteropServices;

public struct Transform
{
    public Vector3 Location;
    public Quaternion Rotation;
    public Vector3 Scale;    
}

// Lion Types
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct cVector
{
    public float x, y, z, w;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct cMatrix
{
    public cVector xa;
    public cVector ya;
    public cVector za;
    public cVector wa;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct cQuat
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public uint[] q;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct cColour8
{
    public uint m_RGBA;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct cTime
{
    public int mTicks;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct sParticleBehaviourBaseVariancePack
{
    public float Base;
    public float Variance;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct cParticleBehaviourBaseVarianceCompiled
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 47)]
    public sParticleBehaviourBaseVariancePack[] aData;
    public uint size;
    public uint dummy;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct cParticleWaveForm
{
    public uint mID, mType;
    public float mBase, mPhase, mFreq, mAmp;
    public float mClampMin, mClampMax;
    public float mBaseVariance, mPhaseVariance;
    public float mFreqVariance, mAmpVariance;
    public float mClampMinVariance, mClampMaxVariance;
}

// Experimenting with a new way to store ResourceIDs.
public struct ResourceID
{
    [Newtonsoft.Json.JsonIgnore]
    public byte[] ID;

    public string HexID
    {
        get => BitConverter.ToString(ID).Replace("-", "").ToLower();
        set => ID = Enumerable.Range(0, value.Length / 2)
                              .Select(x => Convert.ToByte(value.Substring(x * 2, 2), 16))
                              .ToArray();
    }

    public Endian Endian;

    public ResourceID()
    {
        ID = new byte[4];
        Endian = default;
    }
}
