using System.Globalization;

using YamlDotNet.Serialization;

using Volatility.Extensions;

using static Volatility.Utilities.ResourceIDUtilities;

namespace Volatility.Resources;

public struct ResourceImport
{
    // The idea here is that if the name is populated but
    // the ID is empty, the name will be calculated into an ID
    // on export. If both a name and ID exist, use the ID, as
    // this will keep consistency for imported assets. If you
    // want to use the calculated name, clear the ReferenceID field.
    public string Name;
    public ResourceID ReferenceID;
    public bool ExternalImport;

    public ResourceImport() 
    {
        Name = string.Empty;
    }

    public ResourceImport(ResourceID id, bool externalImport = false, bool useCalculatedName = false)
    {
        ReferenceID = id;
        ExternalImport = externalImport;
        Name = GetNameByResourceID(id);

        if (Name.Length > 0 && useCalculatedName)
        {
            ReferenceID = 0x0;
        }
    }

    public ResourceImport(string name, bool externalImport = false)
    {
        Name = name;
        ExternalImport = externalImport;
    }

    public static bool ReadExternalImport(int index, BinaryReader reader, Endian n, long importBlockOffset, out ResourceImport resourceImport)
    {
        long originalPosition = reader.BaseStream.Position;

        // In-resource imports block
        if (reader.BaseStream.Length >= importBlockOffset + (0x10 * index) + 0x10)
        {
            reader.BaseStream.Seek(importBlockOffset + (0x10 * index), SeekOrigin.Begin);

            resourceImport = new ResourceImport(reader.ReadUInt64(n), externalImport: true);
            
            reader.BaseStream.Seek(originalPosition, SeekOrigin.Begin);
            
            return true;
        }

        reader.BaseStream.Seek(originalPosition, SeekOrigin.Begin);

        // YAP imports yaml
        if (reader.BaseStream is FileStream fs)
        {
            string baseName = Path.GetFileNameWithoutExtension(fs.Name);
            string directory = Path.GetDirectoryName(fs.Name);
            string yamlPath = Path.Combine(directory, baseName + "_imports.yaml");

            resourceImport = new ResourceImport(GetYAMLImportValueAt(yamlPath, index), externalImport: true);

            return true;
        }

        resourceImport = default;
        return false;
    }

    public static bool ReadExternalImport(long fileOffset, BinaryReader reader, Endian n, long importBlockOffset, out ResourceImport resourceImport)
    {
        long originalPosition = reader.BaseStream.Position;

        reader.BaseStream.Seek(importBlockOffset, SeekOrigin.Begin);
        
        // In-resource imports block
        while (reader.BaseStream.Position + 0x10 <= reader.BaseStream.Length)
        {
            ulong resourceValue = reader.ReadUInt64(n);
            long entryKey = reader.ReadUInt32(n);

            if (entryKey == fileOffset)
            {
                resourceImport = new ResourceImport(resourceValue, externalImport: true);
                reader.BaseStream.Seek(originalPosition, SeekOrigin.Begin);
                return true;
            }
        }

        reader.BaseStream.Seek(originalPosition, SeekOrigin.Begin);
        
        // YAP imports yaml
        if (reader.BaseStream is FileStream fs)
        {
            string baseName = Path.GetFileNameWithoutExtension(fs.Name);
            string directory = Path.GetDirectoryName(fs.Name);
            string yamlPath = Path.Combine(directory, baseName + "_imports.yaml");

            if (File.Exists(yamlPath))
            {
                ulong? yamlValue = GetYAMLImportValueByKey(yamlPath, fileOffset);
                if (yamlValue.HasValue)
                {
                    resourceImport = new ResourceImport(yamlValue.Value, externalImport: true);
                    return true;
                }
            }
        }

        resourceImport = default;
        return false;
    }

    public static ResourceID GetYAMLImportValueAt(string yamlPath, int index)
    {
        var yaml = File.ReadAllText(yamlPath);
        var deser = new DeserializerBuilder().Build();

        var list = deser
            .Deserialize<List<Dictionary<string, string>>>(yaml)
            ?? throw new InvalidDataException("Expected a YAML sequence of mappings.");

        if (index < 0 || index >= list.Count)
            throw new ArgumentOutOfRangeException(nameof(index), $"Valid range 0–{list.Count - 1}");

        var kv = list[index].Values.GetEnumerator();
        kv.MoveNext();
        return Convert.ToUInt32(kv.Current, 16);
    }

    public static ResourceID GetYAMLImportValueByKey(string yamlPath, long fileOffset)
    {
        var yaml = File.ReadAllText(yamlPath);
        var deser = new DeserializerBuilder().Build();
        var list = deser.Deserialize<List<Dictionary<string, string>>>(yaml);

        string keyStr = $"0x{fileOffset.ToString("x8")}";
        var matchingDict = list.FirstOrDefault(d => d.ContainsKey(keyStr));
        if (matchingDict != null)
        {
            string valueStr = matchingDict[keyStr];
            if (valueStr.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                valueStr = valueStr.Substring(2);
            }
            if (ulong.TryParse(valueStr, NumberStyles.HexNumber, null, out ulong value))
            {
                return value;
            }
        }
        return ResourceID.Default;
    }
};

