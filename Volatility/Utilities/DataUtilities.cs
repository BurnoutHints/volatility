using System.Collections;
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

    public static int Clamp(int value, int min, int max)
    {
        if (value < min)
        {
            return min;
        }
        else if (value > max)
        {
            return max;
        }
        else
        {
            return value;
        }
    }

    public static byte ToNext0x10(byte value) =>
        (value & 0xF) == 0
            ? value
            : (byte)((value + 0xF) & ~0xF);

    public static sbyte ToNext0x10(sbyte value)
    {
        int v = value;
        return (v & 0xF) == 0
            ? value
            : (sbyte)((v + 0xF) & ~0xF);
    }

    public static short ToNext0x10(short value)
    {
        int v = value;
        return (v & 0xF) == 0
            ? value
            : (short)((v + 0xF) & ~0xF);
    }

    public static ushort ToNext0x10(ushort value) =>
        (value & 0xF) == 0
            ? value
            : (ushort)((value + 0xFu) & ~0xFu);

    public static int ToNext0x10(int value) =>
        (value & 0xF) == 0
            ? value
            : (value + 0xF) & ~0xF;

    public static uint ToNext0x10(uint value) =>
        (value & 0xFu) == 0
            ? value
            : (value + 0xFu) & ~0xFu;

    public static long ToNext0x10(long value) =>
        (value & 0xFL) == 0
            ? value
            : (value + 0xFL) & ~0xFL;

    public static ulong ToNext0x10(ulong value) =>
        (value & 0xFul) == 0
            ? value
            : (value + 0xFul) & ~0xFul;
}

