using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YamlDotNet.Serialization;

namespace Volatility.Utilities;

public static class ResourceYamlDeserializer
{
    public static object DeserializeResource(Type resourceClass, string yaml)
    {
var deserializer = new DeserializerBuilder()
    .WithTagMapping("tag:yaml.org,2002:binary", typeof(byte[]))
    .WithTypeConverter(new BinaryTypeConverter())
    .IgnoreUnmatchedProperties()
    .Build();

            
        var root = deserializer.Deserialize<Dictionary<string, object>>(yaml);

        var hierarchyTypes = new List<Type>();
        Type currentType = resourceClass;
        while (currentType != null && currentType != typeof(object))
        {
            hierarchyTypes.Insert(0, currentType);
            currentType = currentType.BaseType;
        }

        string baseKey = hierarchyTypes[0].Name + ".Properties";
        Dictionary<string, object> mergedProperties = new Dictionary<string, object>();
        if (root != null && root.ContainsKey(baseKey))
        {
            mergedProperties = TryConvertToDictionary(root[baseKey]);
        }

        List<string> derivedKeys = new List<string>();
        for (int i = 1; i < hierarchyTypes.Count; i++)
        {
            derivedKeys.Add(hierarchyTypes[i].Name + ".Properties");
        }

        mergedProperties = RemovePropertiesKeys(MergeProperties(mergedProperties, derivedKeys));

        var serializer = new SerializerBuilder().Build();
        string mergedYaml = serializer.Serialize(mergedProperties);

        var finalDeserializer = new DeserializerBuilder()
            .WithTypeConverter(new BinaryTypeConverter())
            .IgnoreUnmatchedProperties()
            .Build();
            
        using (var reader = new StringReader(mergedYaml))
        {
            var resource = finalDeserializer.Deserialize(reader, resourceClass);
            return resource;
        }
    }

    private static Dictionary<string, object> TryConvertToDictionary(object obj)
    {
        if (obj is IDictionary<object, object> genericDict)
        {
            return genericDict.ToDictionary(kvp => kvp.Key.ToString(), kvp => kvp.Value);
        }
        else if (obj is Dictionary<string, object> dict)
        {
            return dict;
        }
        return new Dictionary<string, object>();
    }

    private static Dictionary<string, object> DeepMerge(Dictionary<string, object> target, Dictionary<string, object> source)
    {
        foreach (var kv in source)
        {
            if (target.ContainsKey(kv.Key))
            {
                var targetVal = TryConvertToDictionary(target[kv.Key]);
                var sourceVal = TryConvertToDictionary(kv.Value);
                target[kv.Key] = DeepMerge(targetVal, sourceVal);
            }
            else
            {
                target[kv.Key] = kv.Value;
            }
        }
        return target;
    }

    private static Dictionary<string, object> MergeProperties(Dictionary<string, object> baseDict, List<string> derivedKeys)
    {
        foreach (var key in derivedKeys)
        {
            if (baseDict.ContainsKey(key))
            {
                var derivedDict = TryConvertToDictionary(baseDict[key]);
                baseDict = DeepMerge(baseDict, derivedDict);
                baseDict.Remove(key);
            }
        }
        return baseDict;
    }

    private static Dictionary<string, object> RemovePropertiesKeys(Dictionary<string, object> dict)
    {
        return dict.Where(kvp => !kvp.Key.EndsWith(".Properties"))
                   .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }
}
