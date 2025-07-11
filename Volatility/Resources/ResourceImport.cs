using YamlDotNet.Serialization;

namespace Volatility.Resources;

public struct ResourceImport
{
    public ResourceID ReferenceID;
    public bool ExternalImport;

    public static bool ReadExternalImport(byte index, EndianAwareBinaryReader reader, long importBlockOffset, out ResourceImport resourceImport)
    {
        resourceImport.ExternalImport = true;

        // In-resource imports block
        if (reader.BaseStream.Length >= importBlockOffset + (0x10 * index) + 0x10)
        {
            long originalPosition = reader.BaseStream.Position;
            
            reader.BaseStream.Seek(importBlockOffset + (0x10 * index), SeekOrigin.Begin);
            
            resourceImport.ReferenceID = reader.ReadUInt64();
            
            reader.BaseStream.Seek(originalPosition, SeekOrigin.Begin);
            
            return true;
        }
        // YAP imports yaml
        else if (reader.BaseStream is FileStream fs)
        {
            string baseName = Path.GetFileNameWithoutExtension(fs.Name);

            string directory = Path.GetDirectoryName(fs.Name);

            resourceImport.ReferenceID = GetYAMLImportValueAt(Path.Combine(directory, baseName + "_imports.yaml"), index);

            return true;
        }

        resourceImport = default;
        return false;
    }


    public static ResourceID GetYAMLImportValueAt(string yamlPath, byte index)
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
};

