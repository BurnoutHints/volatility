using System.Text;

using Volatility.Resources;

namespace Volatility.Utilities;

public class ResourceUtilities
{
    public static int GetPointerSize(Arch arch)
    {
        return arch == Arch.x64 ? sizeof(ulong) : sizeof(uint);
    }

    public static List<T> GetFixedSizeList<T>(List<T> source, int size)
    {
        List<T> output = new(size);
        for (int i = 0; i < size; i++)
        {
            output.Add(i < source.Count ? source[i] : default!);
        }

        return output;
    }

    public static long AlignOffset(long offset, int alignment)
    {
        return PaddingUtilities.GetPaddedLength(offset, alignment);
    }

    public static ulong GetSectionOffset(ref long currentOffset, int count, int elementSize, int sectionAlignment)
    {
        if (count <= 0)
        {
            return 0;
        }

        currentOffset = AlignOffset(currentOffset, sectionAlignment);
        ulong offset = (ulong)currentOffset;
        currentOffset += (long)count * elementSize;
        return offset;
    }

    public static long GetSectionOffset(ref long currentOffset, int length, int sectionAlignment)
    {
        if (length <= 0)
        {
            return 0;
        }

        currentOffset = AlignOffset(currentOffset, sectionAlignment);
        long offset = currentOffset;
        currentOffset += length;
        return offset;
    }

    public static string ReadFixedString(BinaryReader reader, int length)
    {
        byte[] bytes = reader.ReadBytes(length);
        int nullTerminator = Array.IndexOf(bytes, (byte)0);
        int outputLength = nullTerminator >= 0 ? nullTerminator : bytes.Length;
        return Encoding.ASCII.GetString(bytes, 0, outputLength);
    }

    public static void WriteFixedString(BinaryWriter writer, string? value, int length)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(value ?? string.Empty);
        if (bytes.Length >= length)
        {
            writer.Write(bytes, 0, length);
            return;
        }

        writer.Write(bytes);
        writer.Write(new byte[length - bytes.Length]);
    }

    public static ResourceID ResolveResourceID(ResourceImport resourceImport)
    {
        if (resourceImport.ReferenceID != ResourceID.Default)
        {
            return resourceImport.ReferenceID;
        }

        return string.IsNullOrWhiteSpace(resourceImport.Name)
            ? ResourceID.Default
            : ResourceID.HashFromString(resourceImport.Name);
    }
}
