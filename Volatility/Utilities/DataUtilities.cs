namespace Volatility.Utilities;

public static class DataUtilities
{
    public static byte TrimIntToByte(int input)
    {
        return BitConverter.GetBytes(input)[0];
    }

    public static byte[] x64Switch(bool x64, ulong value)
    {
        return x64 ? BitConverter.GetBytes(value) : BitConverter.GetBytes((uint)value);
    }

    public static string FlipFileNameEndian(string filePath)
    {
        string baseName = Path.GetFileNameWithoutExtension(filePath);
        string extension = Path.GetExtension(filePath);

        string[] segments = baseName.Split('_');

        FlipAssetEndian(segments);

        return string.Join("_", segments) + extension;
    }

    public static byte[] FlipAssetEndian(byte[] CgsIDElements)
    {
        if (CgsIDElements.Length > 4) // Shouldn't usually happen
        {
            Array.Reverse(CgsIDElements, 0, 4);
        }
        else
        {
            Array.Reverse(CgsIDElements);
        }
        return CgsIDElements;
    }

    public static string[] FlipAssetEndian(string[] CgsIDElements)
    {
        if (CgsIDElements.Length > 4) // File names & properties
        {
            Array.Reverse(CgsIDElements, 0, 4);
        }
        else
        {
            Array.Reverse(CgsIDElements);
        }
        return CgsIDElements;
    }

    public static int CalculatePitch(int width, int blockSize)
    {
        int adjustedWidth = (width + 3) / 4;
        int pitch = adjustedWidth * blockSize;
        return pitch;
    }

    public static bool IsPowerOfTwo(int x)
    {
        return (x > 0) && ((x & (x - 1)) == 0);
    }
}
