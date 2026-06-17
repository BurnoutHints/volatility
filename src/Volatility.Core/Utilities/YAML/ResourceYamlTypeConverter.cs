using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

using Volatility.Resources;

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

    public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer nestedObjectSerializer)
    {
        if (value == null) return;

        var hierarchyTypes = new List<Type>();
        Type? currentType = value.GetType();
        while (currentType != null && Accepts(currentType))
        {
            hierarchyTypes.Add(currentType);
            currentType = currentType.BaseType;
        }
        hierarchyTypes.Reverse();

        var processedMembers = new HashSet<string>();

        var root = new Dictionary<string, object?>();
        Dictionary<string, object?>? currentDict = null;

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
                if (currentDict != null)
                {
                    currentDict[key] = typeProperties;
                }
                currentDict = typeProperties;
            }
        }

        nestedObjectSerializer(root);
    }

    private void WriteValue(IEmitter emitter, object? value)
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
                emitter.Emit(new Scalar(entry.Key.ToString() ?? string.Empty));
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
            emitter.Emit(new Scalar(value.ToString() ?? string.Empty));
        }
    }
}