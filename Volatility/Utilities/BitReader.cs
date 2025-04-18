using System.Collections;

namespace Volatility.Utilities;

public class BitReader : IDisposable
{
    private readonly byte[] buffer;
    private int currentBit;

    public BitReader(byte[] data) => buffer = data;

    public void Dispose()
    { 
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    private bool[] ReadBitsInternal(int count)
    {
        bool[] bits = new bool[count];
        int bitsRead = 0;

        while (bitsRead < count)
        {
            int byteIndex = currentBit / 8;
            int bitIndex = 7 - (currentBit % 8);
            int bitsLeft = count - bitsRead;
            int bitsToRead = Math.Min(bitsLeft, bitIndex + 1);
            
            for (int i = 0; i < bitsToRead; ++i)
            {
                bits[bitsRead + i] = (buffer[byteIndex] & (1 << (bitIndex - i))) != 0;
            }

            bitsRead += bitsToRead;
            currentBit += bitsToRead;
        }

        return bits;
    }

    public uint ReadBitsToUInt(int count)
    {
        bool[] bits = ReadBitsInternal(count);
        uint result = 0;
        for (int i = 0; i < count; i++)
        {
            if (bits[i])
            {
                result |= (uint)(1 << (count - 1 - i));
            }
        }
        return result;
    }

    public BitArray ReadBitsToBitArray(int count)
    {
        bool[] bits = ReadBitsInternal(count);
        return new BitArray(bits);
    }

    public void Seek(int offset, SeekOrigin origin)
    {
        switch (origin)
        {
            case SeekOrigin.Begin:
                currentBit = offset;
                break;
            case SeekOrigin.Current:
                currentBit += offset;
                break;
            case SeekOrigin.End:
                currentBit = buffer.Length * 8 - offset;
                break;
            default:
                throw new ArgumentException("Invalid seek origin!", nameof(origin));
        }

        if (currentBit < 0 || currentBit > buffer.Length * 8)
        {
            throw new ArgumentOutOfRangeException("Seek position is outside the buffer range.");
        }
    }
}
