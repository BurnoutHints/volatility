// Temporary Types
global using ModelPtr = System.IntPtr;

// Permenant Types
global using Vector2 = System.Numerics.Vector2;         // VectorIntrinsic
global using Vector3 = System.Numerics.Vector3;         // VectorIntrinsic
global using Vector3Plus = System.Numerics.Vector4;     // VectorIntrinsic
global using Vector4 = System.Numerics.Vector4;
using System.Numerics;         // VectorIntrinsic

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

    public static Transform ReadMatrix44AffineAsTransform(EndianAwareBinaryReader reader)
    {
        Transform transform = new Transform();
        
        transform.Location.X = reader.ReadSingle();
        transform.Rotation.X = reader.ReadSingle();
        transform.Scale.X = reader.ReadSingle();
       
        transform.Location.Y = reader.ReadSingle();
        transform.Rotation.Y = reader.ReadSingle();
        transform.Scale.Y = reader.ReadSingle();
        
        transform.Location.Z = reader.ReadSingle();
        transform.Rotation.Z = reader.ReadSingle();
        transform.Scale.Z = reader.ReadSingle();

        // Don't see a reason to preserve W axis for location and scale
        reader.ReadSingle();
        transform.Rotation.W = reader.ReadSingle();
        reader.ReadSingle();

        return transform;
    }
}
    