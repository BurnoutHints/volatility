using System.Globalization;

using static Volatility.Utilities.DataUtilities;

namespace Volatility.Utilities;

public static class ResourceIDUtilities
{
    public static string FlipPathResourceIDEndian(string filePath)
    {
        string extension = Path.GetExtension(filePath);

        return string.Join("_", FlipResourceIDEndian(PathToResourceID(filePath))) + extension;
    }

    public static string[] PathToResourceID(string filePath, bool strip = false)
    {
        string baseName = Path.GetFileNameWithoutExtension(filePath);
        string[] split = baseName.Split("_");

        if (!strip)
            return split;

        string[] firstFour = new string[4];
        Array.Copy(split, firstFour, 4);

        return firstFour;
    }

    public static string[] ResourceNameToResourceID(string id)
    {
        return Enumerable.Range(0, id.Length / 2).Select(i => id.Substring(i * 2, 2)).ToArray();
    }

    public static bool ValidateResourceID(string resourceID)
    {
        string[] id = PathToResourceID(resourceID);

        if (id.Length != 4)
        {
            if (id.Length == 1)
            {
                return IsHexadecimal(id[0]) && (id[0].Length == 0x8 || id[0].Length == 0x10);
            }

            return false;
        }

        foreach (string part in id)
        {
            if (part.Length != 2 || !IsHexadecimal(part))
            {
                return false;
            }
        }

        return true;
    }

    public static bool TryParseResourceID(string input, out ResourceID resourceID)
    {
        resourceID = ResourceID.Default;

        if (string.IsNullOrWhiteSpace(input))
            return false;

        string trimmed = input.Trim().Replace("_", string.Empty, StringComparison.Ordinal);
        if (trimmed.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            trimmed = trimmed[2..];

        if (trimmed.Length == 0)
            return false;

        if (IsHexadecimal(trimmed)
            && ulong.TryParse(trimmed, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ulong hexValue))
        {
            resourceID = hexValue;
            return true;
        }

        if (ulong.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out ulong decValue))
        {
            resourceID = decValue;
            return true;
        }

        return false;
    }

    public static byte[] FlipResourceIDEndian(byte[] resourceIDElements)
    {
        if (resourceIDElements.Length > 4)
        {
            Array.Reverse(resourceIDElements, 0, 4);
        }
        else
        {
            Array.Reverse(resourceIDElements);
        }

        return resourceIDElements;
    }

    public static string[] FlipResourceIDEndian(string[] resourceIDElements)
    {
        if (resourceIDElements.Length > 4)
        {
            Array.Reverse(resourceIDElements, 0, 4);
        }
        else
        {
            Array.Reverse(resourceIDElements);
        }

        return resourceIDElements;
    }

    public static string FlipResourceIDEndian(string resourceID)
    {
        return string.Concat(FlipResourceIDEndian(ResourceNameToResourceID(resourceID)));
    }
}
