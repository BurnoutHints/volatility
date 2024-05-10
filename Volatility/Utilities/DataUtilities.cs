using System.Collections;
using System.Text;
using System.Text.RegularExpressions;

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
        return (ushort)((width > height ? width : height) / 32);
    }

    public static bool IsPowerOfTwo(int x)
    {
        return (x > 0) && ((x & (x - 1)) == 0);
    }

    public static int SwapEndian(int value)
    {
        return (int)SwapEndian((uint)value);
    }

    public static uint SwapEndian(uint value)
    {
        return ((value & 0x000000FF) << 24) | 
               ((value & 0x0000FF00) << 8)  | 
               ((value & 0x00FF0000) >> 8)  | 
               ((value & 0xFF000000) >> 24);
    }

    public static ushort SwapEndian(ushort value)
    {
        return (ushort)(((value & 0x00FF) << 8) | 
                        ((value & 0xFF00) >> 8));
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
}
