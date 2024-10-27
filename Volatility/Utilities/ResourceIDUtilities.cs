using System.IO.Hashing;
using System.Text;

using Newtonsoft.Json;

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

    public static bool ValidateResourceID(string ResourceID)
    {
        string[] id = PathToResourceID(ResourceID);

        if (id.Length != 4)
            return false;

        foreach (string part in id)
        {
            if (part.Length != 2 || !IsHexadecimal(part))
                return false;
        }

        return true;
    }

    public static byte[] FlipResourceIDEndian(byte[] ResourceIDElements)
    {
        if (ResourceIDElements.Length > 4) // Shouldn't usually happen
        {
            Array.Reverse(ResourceIDElements, 0, 4);
        }
        else
        {
            Array.Reverse(ResourceIDElements);
        }
        return ResourceIDElements;
    }

    public static string[] FlipResourceIDEndian(string[] ResourceIDElements)
    {
        if (ResourceIDElements.Length > 4) // File names & properties
        {
            Array.Reverse(ResourceIDElements, 0, 4);
        }
        else
        {
            Array.Reverse(ResourceIDElements);
        }
        return ResourceIDElements;
    }

    public static string FlipResourceIDEndian(string ResourceID)
    {
        return string.Concat(FlipResourceIDEndian(ResourceNameToResourceID(ResourceID)));
    }

    public static string GetNameByResourceID(string id, string type)
    {
        string path = Path.Combine
        (
            Directory.GetCurrentDirectory(), 
            "data", 
            "ResourceDB", 
            $"{type}.json"
        );

        if (File.Exists(path))
        {
            Dictionary<string, string>? data = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(path));

            return data.TryGetValue(id.Replace("_", "").ToLower(), out string? value) ? value : "";
        }

        return "";
    }

    public static string GetResourceIDFromName(string name, Endian endian = Endian.LE)
    {
        byte[] hash = Crc32.Hash(Encoding.UTF8.GetBytes(name.ToLower()));

        if (endian == Endian.BE) 
        {
            Array.Reverse(hash);
        }

        return BitConverter.ToString(hash).Replace("-", "_").ToUpper();
    }
}
