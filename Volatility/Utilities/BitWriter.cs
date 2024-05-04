namespace Volatility.Utilities;

public class BitWriter
{
    private byte[] buffer;
    private int currentBit;
    
    public BitWriter(int size)
    {
        buffer = new byte[size];
        currentBit = 0;
    }

    public BitWriter(byte[] array)
    {
        buffer = array;
        currentBit = 0;
    }

    public void Write(uint value, int bitCount)
    {
        for (int i = 0; i < bitCount; i++)
        {
            int byteIndex = currentBit / 8;
            int bitIndex = currentBit % 8;
            if ((value & (1 << i)) != 0)
            {
                buffer[byteIndex] |= (byte)(1 << (7 - bitIndex));
            }
            currentBit++;
        }
    }

    public byte[] ToArray()
    {
        return buffer;
    }

    public void Seek(uint Position) 
    {
        currentBit = (int)Position;
    }
}
