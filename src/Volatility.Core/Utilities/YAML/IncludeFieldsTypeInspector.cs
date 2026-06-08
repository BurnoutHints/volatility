using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.TypeInspectors;

namespace Volatility.Utilities;

public class IncludeFieldsTypeInspector : TypeInspectorSkeleton
{
    private readonly ITypeInspector innerTypeInspector;

    public IncludeFieldsTypeInspector(ITypeInspector innerTypeInspector)
    {
        this.innerTypeInspector = innerTypeInspector;
    }

    public override IEnumerable<IPropertyDescriptor> GetProperties(Type type, object container)
    {
        var descriptors = innerTypeInspector.GetProperties(type, container).ToList();
        var existingNames = new HashSet<string>(descriptors.Select(d => d.Name));
        foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public))
        {
            if (!existingNames.Contains(field.Name))
            {
                descriptors.Add(new FieldDescriptor(field));
            }
        }

        return descriptors;
    }

    public override string GetEnumValue(object value)
    {
        return value.ToString();
    }

    public override string GetEnumName(Type enumType, string value)
    {
        return value;
    }
}