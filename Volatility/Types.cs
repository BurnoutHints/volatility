// Temporary Types
global using ModelPtr = System.IntPtr;

// Permenant Types
global using Vector2 = System.Numerics.Vector2;         // VectorIntrinsic
global using Vector3 = System.Numerics.Vector3;         // VectorIntrinsic
global using Vector3Plus = System.Numerics.Vector4;     // VectorIntrinsic
global using Vector4 = System.Numerics.Vector4;
global using Quaternion = System.Numerics.Quaternion;
global using Matrix44Affine = System.Numerics.Matrix4x4;

public enum Endian
{
    LE,
    BE
}

public struct Transform
{
    public Vector3 Location;
    public Quaternion Rotation;
    public Vector3 Scale;    
}
    