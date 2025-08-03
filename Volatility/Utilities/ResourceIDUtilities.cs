using Newtonsoft.Json;

using static Volatility.Utilities.DataUtilities;
using static Volatility.Utilities.EnvironmentUtilities;

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

    public static string GetNameByResourceID(string id)
    {
        string path = Path.Combine
        (
            GetEnvironmentDirectory(EnvironmentDirectory.ResourceDB),
            "ResourceDB.json"
        );

        if (File.Exists(path))
        {
            Dictionary<string, string>? data = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(path));

            return data.TryGetValue(id.Replace("_", "").ToLower(), out string? value) ? value : "";
        }

        return "";
    }

    public static string GetNameByResourceID(ResourceID id)
    {
        string path = Path.Combine
        (
            GetEnvironmentDirectory(EnvironmentDirectory.ResourceDB),
            "ResourceDB.json"
        );

        if (File.Exists(path))
        {
            Dictionary<string, string>? data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            using var reader = File.OpenText(path);
            using var json = new JsonTextReader(reader);
            new JsonSerializer()
                .Populate(json, data);

            return data.TryGetValue(id.ToString(), out string? value) ? value : "";
        }

        return "";
    }
}
