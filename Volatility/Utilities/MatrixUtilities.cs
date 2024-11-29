namespace Volatility.Utilities;

public static class MatrixUtilities
{
    public static Transform Matrix44AffineToTransform(Matrix44Affine matrix)
    {
        Transform transform = new Transform();

        transform.Location = new Vector3(matrix.M41, matrix.M42, matrix.M43);
        transform.Scale = new Vector3(
           (float)Math.Sqrt(matrix.M11 * matrix.M11 + matrix.M12 * matrix.M12 + matrix.M13 * matrix.M13),
           (float)Math.Sqrt(matrix.M21 * matrix.M21 + matrix.M22 * matrix.M22 + matrix.M23 * matrix.M23),
           (float)Math.Sqrt(matrix.M31 * matrix.M31 + matrix.M32 * matrix.M32 + matrix.M33 * matrix.M33) 
        );

        float m11 = matrix.M11 / transform.Scale.X;
        float m12 = matrix.M12 / transform.Scale.X;
        float m13 = matrix.M13 / transform.Scale.X;

        float m21 = matrix.M21 / transform.Scale.Y;
        float m22 = matrix.M22 / transform.Scale.Y;
        float m23 = matrix.M23 / transform.Scale.Y;

        float m31 = matrix.M31 / transform.Scale.Z;
        float m32 = matrix.M32 / transform.Scale.Z;
        float m33 = matrix.M33 / transform.Scale.Z;

        transform.Rotation = RotationMatrixToQuaternion(
            m11, m12, m13,
            m21, m22, m23,
            m31, m32, m33
        );

        return transform;
    }

    public static Matrix44Affine ReadMatrix44Affine(BinaryReader reader)
    {
        float m11 = reader.ReadSingle();
        float m12 = reader.ReadSingle();
        float m13 = reader.ReadSingle();
        float m14 = reader.ReadSingle();
        float m21 = reader.ReadSingle();
        float m22 = reader.ReadSingle();
        float m23 = reader.ReadSingle();
        float m24 = reader.ReadSingle();
        float m31 = reader.ReadSingle();
        float m32 = reader.ReadSingle();
        float m33 = reader.ReadSingle();
        float m34 = reader.ReadSingle();
        float m41 = reader.ReadSingle();
        float m42 = reader.ReadSingle();
        float m43 = reader.ReadSingle();
        float m44 = reader.ReadSingle();

        return new Matrix44Affine(
            m11, m12, m13, m14,
            m21, m22, m23, m24,
            m31, m32, m33, m34,
            m41, m42, m43, m44
        );
    }

    public static Quaternion RotationMatrixToQuaternion(
        float m00, float m01, float m02,
        float m10, float m11, float m12,
        float m20, float m21, float m22)
    {

        float trace = m00 + m11 + m22;
        float qw, qx, qy, qz;

        if (trace > 0)
        {
            float s = (float)Math.Sqrt(trace + 1.0f) * 2;
            qw = 0.25f * s;
            qx = (m21 - m12) / s;
            qy = (m02 - m20) / s;
            qz = (m10 - m01) / s;
        }
        else if ((m00 > m11) && (m00 > m22))
        {
            float s = (float)Math.Sqrt(1.0f + m00 - m11 - m22) * 2;
            qw = (m21 - m12) / s;
            qx = 0.25f * s;
            qy = (m01 + m10) / s;
            qz = (m02 + m20) / s;
        }
        else if (m11 > m22)
        {
            float s = (float)Math.Sqrt(1.0f + m11 - m00 - m22) * 2;
            qw = (m02 - m20) / s;
            qx = (m01 + m10) / s;
            qy = 0.25f * s;
            qz = (m12 + m21) / s;
        }
        else
        {
            float s = (float)Math.Sqrt(1.0f + m22 - m00 - m11) * 2;
            qw = (m10 - m01) / s;
            qx = (m02 + m20) / s;
            qy = (m12 + m21) / s;
            qz = 0.25f * s;
        }

        Quaternion quaternion = new Quaternion(qx, qy, qz, qw);
        quaternion = Quaternion.Normalize(quaternion);

        return quaternion;
    }
}
