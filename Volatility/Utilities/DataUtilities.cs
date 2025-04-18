using System.Collections;
using System.Globalization;
using System.Text;

namespace Volatility.Utilities;

public static class DataUtilities
{
    public static byte TrimIntToByte(int input)
    {
        return BitConverter.GetBytes(input)[0];
    }

    public static byte[] x64Switch(bool x64, ulong value)
    {
        return x64 ? BitConverter.GetBytes(value) : BitConverter.GetBytes((uint)value);
    }

    public static bool IsHexadecimal(string input)
    {
        foreach (char c in input)
        {
            if (!((c >= '0' && c <= '9') || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f')))
                return false;
        }
        return true;
    }

    public static int CalculatePitchPS3(int width, int blockSize)
    {
        return ((width + 3) / 4) * blockSize;
    }

    public static ushort CalculatePitchX360(ushort width, ushort height)
    {
        return (ushort)(Clamp(width, 128, width) / 32);
    }

    public static uint CalculateMipAddressX360(uint width, uint height)
    {
        return (width * height) / 4096;
    }

    public static bool IsPowerOfTwo(int x)
    {
        return (x > 0) && ((x & (x - 1)) == 0);
    }

    public static string ConcatBitString(string inString, int value, int bits)
    {
        return inString += Convert.ToString(value, 2).PadLeft(bits, '0');
    }
    
    public static string ConcatBitString(string inString, uint value, int bits)
    {
        return inString += Convert.ToString(value, 2).PadLeft(bits, '0');
    }

    public static string ConcatBitString(string inString, bool value, int bits)
    {
        return ConcatBitString(inString, (value ? 1 : 0), bits);
    }
    public static void ConcatBitString(StringBuilder sb, int value, int bits)
    {
        sb.Append(Convert.ToString(value, 2).PadLeft(bits, '0'));
    }

    public static void ConcatBitString(StringBuilder sb, uint value, int bits)
    {
        sb.Append(Convert.ToString(value, 2).PadLeft(bits, '0'));
    }

    public static void ConcatBitString(StringBuilder sb, bool value, int bits)
    {
        ConcatBitString(sb, value ? 1 : 0, bits);
    }

    public static byte[] BinaryStringToBytes(string inString, int byteLength) 
    {
        byte[] bytes = new byte[byteLength];
        for (int i = 0; i < byteLength; i++)
        {
            bytes[i] = Convert.ToByte(inString.Substring(i * 8, 8), 2);
        }
        return bytes;
    }

    public static bool IsComplexType(Type type)
    {
        return !type.IsPrimitive && !type.IsEnum && type != typeof(string) && !type.IsArray && type != typeof(BitArray);
    }

    public static int Clamp(int value, int min, int max)
    {
        if (value < min)
        {
            return min;
        }
        
        if (value > max)
        {
            return max;
        }

        return value;
    }

    public static bool TryParseEnum<TEnum>(string input, out TEnum result) where TEnum : struct, Enum
    {
        result = default;

        // Hexadecimal input
        if (input.StartsWith("0x"))
        {
            if (int.TryParse(input.Substring(2), NumberStyles.HexNumber, null, out int numericValue))
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

