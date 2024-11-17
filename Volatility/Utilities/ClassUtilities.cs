using System.Reflection;

namespace Volatility.Utilities;

public static class ClassUtilities
{
    public static string GetStaticPropertyValue(Type type, string propertyName)
    {
        PropertyInfo property = type.GetProperty(propertyName, BindingFlags.Static | BindingFlags.Public);
        if (property != null && property.PropertyType == typeof(string))
        {
            return (string)property.GetValue(null);
        }
        return null;
    }

    public static Type[] GetDerivedTypes(Type baseType)
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => baseType.IsAssignableFrom(type) && type.IsClass && !type.IsAbstract)
            .ToArray();
    }

    public static T? GetAttribute<T>(MemberInfo member) where T : Attribute
    {
        return member.GetCustomAttribute<T>();
    }
    
    public static bool IsComplexType(object value)
    {
        var type = value.GetType();
        return !type.IsPrimitive && 
               !(value is string) &&
               !type.IsEnum &&
               !typeof(System.Collections.IEnumerable).IsAssignableFrom(type);
    }
}
