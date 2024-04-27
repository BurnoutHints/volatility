namespace Volatility.Utilities;

public class BitReader
{
    private readonly byte[] buffer;
    private int currentBit = 0;

    public BitReader(byte[] data) => buffer = data;

    public uint ReadBits(int count)
    {
        // Can't read more than 32 bits at a time
        if (count < 0 || count > 32)
        {
            throw new ArgumentOutOfRangeException("count", "Count must be between 0 and 32.");
        }

        uint result = 0;
        int bitsRead = 0;

        while (bitsRead < count)
        {
            int byteIndex = currentBit / 8;
            int bitIndex = 7 - (currentBit % 8); // Most significant bit
            int bitsLeft = count - bitsRead;
            int bitsToRead = Math.Min(bitsLeft, 8 - bitIndex);

            uint mask = (uint)((1 << bitsToRead) - 1);
            result |= (uint)(((buffer[byteIndex] >> (bitIndex + 1 - bitsToRead)) & mask) << bitsRead);

            bitsRead += bitsToRead;
            currentBit += bitsToRead;
        }

        return result;
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
