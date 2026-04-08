namespace Volatility.Utilities;

public class ResourceUtilities
{
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
}
