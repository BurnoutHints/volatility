using YamlDotNet.Serialization;

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

    public ResourceImport() { }

    public ResourceImport(ResourceID id, bool externalImport = false, bool useCalculatedName = false)
    {
        ReferenceID = id;
        ExternalImport = externalImport;
        // TODO: find name in ResourceDB, pending ResourceDB v3
        // if (useCalculatedName || resource was found in db)
        //     ReferenceID = 0x0;
    }

    public ResourceImport(string name, bool externalImport = false)
    {
        Name = name;
        ExternalImport = externalImport;
    }

    public static bool ReadExternalImport(byte index, EndianAwareBinaryReader reader, long importBlockOffset, out ResourceImport resourceImport)
    {
        // In-resource imports block
        if (reader.BaseStream.Length >= importBlockOffset + (0x10 * index) + 0x10)
        {
            long originalPosition = reader.BaseStream.Position;
            
            reader.BaseStream.Seek(importBlockOffset + (0x10 * index), SeekOrigin.Begin);

            resourceImport = new ResourceImport(reader.ReadUInt64(), externalImport: true);
            
            reader.BaseStream.Seek(originalPosition, SeekOrigin.Begin);
            
            return true;
        }
        // YAP imports yaml
        else if (reader.BaseStream is FileStream fs)
        {
            string baseName = Path.GetFileNameWithoutExtension(fs.Name);

            string directory = Path.GetDirectoryName(fs.Name);

            resourceImport = new ResourceImport(GetYAMLImportValueAt(Path.Combine(directory, baseName + "_imports.yaml"), index), externalImport: true);

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

