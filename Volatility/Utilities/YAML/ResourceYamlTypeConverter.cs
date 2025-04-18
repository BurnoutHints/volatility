using System.Collections;
using System.Reflection;

using Volatility.Resources;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Volatility.Utilities;

public class ResourceYamlTypeConverter : IYamlTypeConverter
{
    public bool Accepts(Type type)
    {
        return typeof(Resource).IsAssignableFrom(type);
    }

    public object ReadYaml(IParser parser, Type type, ObjectDeserializer nestedObjectDeserializer)
    {
        throw new NotImplementedException();
    }

    public void WriteYaml(IEmitter emitter, object value, Type type, ObjectSerializer nestedObjectSerializer)
    {
        var hierarchyTypes = new List<Type>();
        Type currentType = value.GetType();
        while (currentType != null && Accepts(currentType))
        {
            hierarchyTypes.Add(currentType);
            currentType = currentType.BaseType;
        }
        hierarchyTypes.Reverse();

        var processedMembers = new HashSet<string>();

        Dictionary<string, object> root = new Dictionary<string, object>();
        Dictionary<string, object>? currentDict = null;

        for (int i = 0; i < hierarchyTypes.Count; i++)
        {
            Type t = hierarchyTypes[i];
            var typeProperties = new Dictionary<string, object?>();

            var members = t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Cast<MemberInfo>()
                .Concat(t.GetFields(BindingFlags.Public | BindingFlags.Instance));

            foreach (var member in members)
            {
                if (!processedMembers.Contains(member.Name))
                {
                    object? memberValue = null;
                    if (member.MemberType == MemberTypes.Property)
                    {
                        memberValue = ((PropertyInfo)member).GetValue(value);
                    }
                    else if (member.MemberType == MemberTypes.Field)
                    {
                        memberValue = ((FieldInfo)member).GetValue(value);
                    }
                    typeProperties[member.Name] = memberValue;
                    processedMembers.Add(member.Name);
                }
            }

            string key = t.Name + ".Properties";

            if (i == 0)
            {
                root[key] = typeProperties;
                currentDict = typeProperties;
            }
            else
            {
                currentDict[key] = typeProperties;
                currentDict = typeProperties;
            }
        }

        nestedObjectSerializer(root);
    }

    private void WriteValue(IEmitter emitter, object value)
    {
        if (value == null)
        {
            emitter.Emit(new Scalar("null"));
        }
        else if (value is IDictionary dict)
        {
            emitter.Emit(new MappingStart(null, null, false, MappingStyle.Block));
            foreach (DictionaryEntry entry in dict)
            {
                emitter.Emit(new Scalar(entry.Key.ToString()));
                WriteValue(emitter, entry.Value);
            }
            emitter.Emit(new MappingEnd());
        }
        else if (value is IEnumerable enumerable && !(value is string))
        {
            emitter.Emit(new SequenceStart(null, null, false, SequenceStyle.Block));
            foreach (var item in enumerable)
            {
                WriteValue(emitter, item);
            }
            emitter.Emit(new SequenceEnd());
        }
        else
        {
            emitter.Emit(new Scalar(value.ToString()));
        }
    }

    // This method now expects the YAML to have a nested structure.
    public static object DeserializeResource(Type resourceClass, string yaml)
    {
        var deserializer = new DeserializerBuilder().Build();
        var root = deserializer.Deserialize<Dictionary<string, object>>(yaml);

        var hierarchyTypes = new List<Type>();
        Type currentType = resourceClass;
        while (currentType != null && currentType != typeof(object))
        {
            hierarchyTypes.Insert(0, currentType);
            currentType = currentType.BaseType;
        }

        Dictionary<string, object> mergedProperties = new Dictionary<string, object>();
        if (root != null)
        {
            string baseKey = hierarchyTypes[0].Name + ".Properties";
            if (root.ContainsKey(baseKey))
            {
                mergedProperties = TryConvertToDictionary(root[baseKey]);
            }
        }

        Dictionary<string, object> currentDict = mergedProperties;
        for (int i = 1; i < hierarchyTypes.Count; i++)
        {
            string key = hierarchyTypes[i].Name + ".Properties";
            if (currentDict != null && currentDict.ContainsKey(key))
            {
                var nestedDict = TryConvertToDictionary(currentDict[key]);
                foreach (var kv in nestedDict)
                {
                    currentDict[kv.Key] = kv.Value;
                }
                currentDict = nestedDict;
            }
        }

        var serializer = new SerializerBuilder().Build();
        string mergedYaml = serializer.Serialize(mergedProperties);
        var finalDeserializer = new DeserializerBuilder().Build();
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

        if (obj is Dictionary<string, object> dict)
        {
            return dict;
        }
        return new Dictionary<string, object>();
    }
}