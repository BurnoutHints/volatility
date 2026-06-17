global using CgsID = System.UInt64;

namespace Volatility.Utilities;

public static class CgsIDUtilities
{
    public static CgsID Encode(string id)
    {
        if (string.IsNullOrEmpty(id) || id.Length > 12) return 0UL;
        ulong encoded = 0;
        for (int i = 0; i < 12; ++i)
        {
            char chr = i < id.Length ? id[i] : (char)0;
            if (chr == 0) chr = (char)32;
            if (chr == 95) encoded = encoded * 40 + 39;
            else if (chr >= 65) encoded = encoded * 40 + (ulong)(chr - 52);
            else if (chr >= 48) encoded = encoded * 40 + (ulong)(chr - 45);
            else if (chr >= 47) encoded = encoded * 40 + 2;
            else if (chr >= 45) encoded = encoded * 40 + 1;
            else encoded *= 40;
        }
        return encoded;
    }

    public static string Decode(ulong id)
    {
        if (id == 0UL) return "Invalid ID";
        char[] buf = new char[12];
        for (int i = 11; i >= 0; --i)
        {
            ulong mod = id % 40UL;
            char c;
            if (mod == 39UL) c = '_';
            else if (mod >= 13UL) c = (char)(mod + 52UL);
            else if (mod >= 3UL) c = (char)(mod + 45UL);
            else if (mod >= 2UL) c = '/';
            else { mod = (mod - 1UL) & 32UL; c = (char)mod; }
            buf[i] = c;
            id /= 40UL;
        }
        return new string(buf).TrimEnd(' ');
    }
}