using System.Reflection;
using Volatility.Utilities;

namespace Volatility.Core.Utilities;

public record PropertyMismatch(string Path, object? Exported, object? Imported);

public static class ResourcePropertyComparer
{
    public static List<PropertyMismatch> Compare(object exported, object imported)
    {
        var mismatches = new List<PropertyMismatch>();
        CompareRecursive(exported, imported, "", mismatches);
        return mismatches;
    }

    private static void CompareRecursive(object? exported, object? imported, string prefix, List<PropertyMismatch> mismatches)
    {
        if (exported == null || imported == null)
        {
            if (exported != imported)
            {
                mismatches.Add(new PropertyMismatch(prefix, exported, imported));
            }
            return;
        }

        Type type = exported.GetType();
        PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (PropertyInfo property in properties)
        {
            if (property.GetIndexParameters().Length > 0) continue;

            object? value1 = property.GetValue(exported, null);
            object? value2 = property.GetValue(imported, null);
            string propPath = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}.{property.Name}";

            if (TypeUtilities.IsComplexType(property.PropertyType))
            {
                CompareRecursive(value1, value2, propPath, mismatches);
            }
            else if (!Equals(value1, value2))
            {
                mismatches.Add(new PropertyMismatch(propPath, value1, value2));
            }
        }

        FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
        foreach (FieldInfo field in fields)
        {
            object? value1 = field.GetValue(exported);
            object? value2 = field.GetValue(imported);
            string fieldPath = string.IsNullOrEmpty(prefix) ? field.Name : $"{prefix}.{field.Name}";

            if (TypeUtilities.IsComplexType(field.FieldType))
            {
                CompareRecursive(value1, value2, fieldPath, mismatches);
            }
            else if (!Equals(value1, value2))
            {
                mismatches.Add(new PropertyMismatch(fieldPath, value1, value2));
            }
        }
    }
}
