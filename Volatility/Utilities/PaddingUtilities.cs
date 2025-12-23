using System;
using System.IO;

namespace Volatility.Utilities;

public static class PaddingUtilities
{
    public static int GetPaddingLength(long length, int alignment)
    {
        if (alignment <= 0)
            throw new ArgumentOutOfRangeException(nameof(alignment), "Alignment must be positive.");

        long remainder = length % alignment;
        if (remainder == 0)
            return 0;

        long padding = alignment - remainder;
        return padding > int.MaxValue ? int.MaxValue : (int)padding;
    }

    public static long GetPaddedLength(long length, int alignment)
    {
        return length + GetPaddingLength(length, alignment);
    }

    public static void WritePadding(Stream output, int alignment, long length)
    {
        if (output == null)
            throw new ArgumentNullException(nameof(output));

        int padding = GetPaddingLength(length, alignment);
        if (padding <= 0)
            return;

        Span<byte> zeros = stackalloc byte[0x100];
        while (padding > 0)
        {
            int toWrite = Math.Min(padding, zeros.Length);
            output.Write(zeros[..toWrite]);
            padding -= toWrite;
        }
    }

    public static void WritePadding(Stream output, int alignment)
    {
        if (output == null)
            throw new ArgumentNullException(nameof(output));

        WritePadding(output, alignment, output.Length);
    }
}
