using System.Collections;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Volatility.Utilities;

public static class TypeUtilities
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

    public static Byte[] SerializeStructure<T>(T msg) where T : struct
    {
        int objsize = Marshal.SizeOf<T>();
        byte[] ret = new byte[objsize];
        IntPtr buff = Marshal.AllocHGlobal(objsize);
       
        Marshal.StructureToPtr(msg, buff, true);
        Marshal.Copy(buff, ret, 0, objsize);
        Marshal.FreeHGlobal(buff);
        
        return ret;
    }

    public static bool IsComplexType(Type type)
    {
        return !type.IsPrimitive && !type.IsEnum && type != typeof(string) && !type.IsArray && type != typeof(BitArray);
    }

    public static bool TryParseEnum<TEnum>(string input, out TEnum result) where TEnum : struct, Enum
    {
        result = default;

        // Hexadecimal input
        if (input.StartsWith("0x"))
        {
            if (int.TryParse(input.Substring(2), System.Globalization.NumberStyles.HexNumber, null, out int numericValue))
            {
                return EnumIsDefined(numericValue, out result);
            }
            return false;
        }

        // Nmeric input
        if (int.TryParse(input, out int intValue))
        {
            return EnumIsDefined(intValue, out result);
        }

        // String input
        if (Enum.TryParse(input, true, out TEnum parsedEnum) && Enum.IsDefined(typeof(TEnum), parsedEnum))
        {
            result = parsedEnum;
            return true;
        }

        return false;
    }

    private static bool EnumIsDefined<TEnum>(int value, out TEnum result) where TEnum : struct, Enum
    {
        if (Enum.IsDefined(typeof(TEnum), value))
        {
            result = (TEnum)Enum.ToObject(typeof(TEnum), value);
            return true;
        }

        result = default;
        return false;
    }
}
