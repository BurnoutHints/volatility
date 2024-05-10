using static Volatility.Utilities.DataUtilities;

namespace Volatility.Utilities;

internal class CgsIDUtilities
{
    public static string FlipPathCgsIDEndian(string filePath)
    {
        string extension = Path.GetExtension(filePath);

        return string.Join("_", FlipCgsIDEndian(PathToCgsID(filePath))) + extension;
    }

    public static string[] PathToCgsID(string filePath, bool strip = false)
    {
        string baseName = Path.GetFileNameWithoutExtension(filePath);
        string[] split = baseName.Split("_");

        if (!strip)
            return split;

        string[] firstFour = new string[4];
        Array.Copy(split, firstFour, 4);

        return firstFour;
    }

    public static bool ValidateCgsID(string CgsID)
    {
        string[] id = PathToCgsID(CgsID);

        if (id.Length != 4)
            return false;

        foreach (string part in id)
        {
            if (part.Length != 2 || !IsHexadecimal(part))
                return false;
        }

        return true;
    }

    public static byte[] FlipCgsIDEndian(byte[] CgsIDElements)
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

    public static string[] FlipCgsIDEndian(string[] CgsIDElements)
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
}
