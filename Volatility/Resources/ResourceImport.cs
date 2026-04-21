using System.Globalization;

using YamlDotNet.Serialization;

using static Volatility.Utilities.ResourceIDUtilities;

namespace Volatility.Resources;

public struct ResourceImport
{
    public const int ImportEntrySize = 0x10;

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

    public static string GetImportsPath(string resourcePath, Unpacker unpacker)
    {
        string suffix = unpacker switch
        {
            Unpacker.YAP => "_imports.yaml",
            _ => "_imports.dat",
        };

        return Path.Combine(
            Path.GetDirectoryName(resourcePath) ?? string.Empty,
            Path.GetFileNameWithoutExtension(resourcePath) + suffix);
    }

    public static void DeleteImportsSidecarFiles(string resourcePath)
    {
        DeleteFileIfExists(GetImportsPath(resourcePath, Unpacker.YAP));
        DeleteFileIfExists(GetImportsPath(resourcePath, Unpacker.Raw));
    }

    public static bool ReadExternalImport(int index, EndianAwareBinaryReader reader, long importBlockOffset, out ResourceImport resourceImport)
    {
        long originalPosition = reader.BaseStream.Position;

        // In-resource imports block
        if (reader.BaseStream.Length >= importBlockOffset + ((long)ImportEntrySize * index) + ImportEntrySize)
        {
            reader.BaseStream.Seek(importBlockOffset + ((long)ImportEntrySize * index), SeekOrigin.Begin);

            resourceImport = new ResourceImport(reader.ReadUInt64(), externalImport: true);
            
            reader.BaseStream.Seek(originalPosition, SeekOrigin.Begin);
            
            return true;
        }

        reader.BaseStream.Seek(originalPosition, SeekOrigin.Begin);

        if (reader.BaseStream is FileStream fs)
        {
            if (TryReadBinaryImportAt(GetImportsPath(fs.Name, Unpacker.Raw), reader.Endianness, index, out ResourceID binaryImport))
            {
                resourceImport = new ResourceImport(binaryImport, externalImport: true);
                return true;
            }

            string yamlPath = GetImportsPath(fs.Name, Unpacker.YAP);
            if (File.Exists(yamlPath))
            {
                resourceImport = new ResourceImport(GetYAMLImportValueAt(yamlPath, index), externalImport: true);

                return true;
            }
        }

        resourceImport = default;
        return false;
    }

    public static bool ReadExternalImport(long fileOffset, EndianAwareBinaryReader reader, long importBlockOffset, out ResourceImport resourceImport)
    {
        long originalPosition = reader.BaseStream.Position;

        reader.BaseStream.Seek(importBlockOffset, SeekOrigin.Begin);
        
        // In-resource imports block
        while (reader.BaseStream.Position + ImportEntrySize <= reader.BaseStream.Length)
        {
            ulong resourceValue = reader.ReadUInt64();
            long entryKey = reader.ReadUInt32();

            if (entryKey != fileOffset) continue;
            
            resourceImport = new ResourceImport(resourceValue, externalImport: true);
            reader.BaseStream.Seek(originalPosition, SeekOrigin.Begin);
            return true;
        }

        reader.BaseStream.Seek(originalPosition, SeekOrigin.Begin);
        
        if (reader.BaseStream is FileStream fs)
        {
            if (TryReadBinaryImportByKey(GetImportsPath(fs.Name, Unpacker.Raw), reader.Endianness, fileOffset, out ResourceID binaryImport))
            {
                resourceImport = new ResourceImport(binaryImport, externalImport: true);
                return true;
            }

            string yamlPath = GetImportsPath(fs.Name, Unpacker.YAP);
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

    private static bool TryReadBinaryImportAt(
        string binaryPath,
        Endian endianness,
        int index,
        out ResourceID referenceId)
    {
        referenceId = ResourceID.Default;

        if (index < 0 || !File.Exists(binaryPath))
        {
            return false;
        }

        long entryOffset = (long)ImportEntrySize * index;
        using EndianAwareBinaryReader importReader = new(new FileStream(binaryPath, FileMode.Open, FileAccess.Read, FileShare.Read), endianness);

        if (importReader.BaseStream.Length < entryOffset + ImportEntrySize)
        {
            return false;
        }

        importReader.BaseStream.Seek(entryOffset, SeekOrigin.Begin);
        referenceId = importReader.ReadUInt64();
        return true;
    }

    private static bool TryReadBinaryImportByKey(
        string binaryPath,
        Endian endianness,
        long fileOffset,
        out ResourceID referenceId)
    {
        referenceId = ResourceID.Default;

        if (fileOffset < 0 || fileOffset > uint.MaxValue || !File.Exists(binaryPath))
        {
            return false;
        }

        using EndianAwareBinaryReader importReader = new(new FileStream(binaryPath, FileMode.Open, FileAccess.Read, FileShare.Read), endianness);

        while (importReader.BaseStream.Position + ImportEntrySize <= importReader.BaseStream.Length)
        {
            ulong resourceValue = importReader.ReadUInt64();
            uint entryKey = importReader.ReadUInt32();
            importReader.BaseStream.Seek(sizeof(uint), SeekOrigin.Current);

            if (entryKey != (uint)fileOffset)
            {
                continue;
            }

            referenceId = resourceValue;
            return true;
        }

        return false;
    }

    private static void DeleteFileIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    public static ResourceID GetYAMLImportValueAt(string yamlPath, int index)
    {
        var yaml = File.ReadAllText(yamlPath);
        var deser = new DeserializerBuilder().Build();

        var list = deser
            .Deserialize<List<Dictionary<string, string>>>(yaml)
            ?? throw new InvalidDataException("Expected a YAML sequence of mappings.");

        if (index < 0 || index >= list.Count)
            throw new ArgumentOutOfRangeException(nameof(index), $"Failed to resolve resource import {index}, valid range 0–{list.Count - 1}");

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

