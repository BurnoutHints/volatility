﻿// Permenant Types
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


public struct Transform
{
    public Vector3 Location;
    public Quaternion Rotation;
    public Vector3 Scale;
}