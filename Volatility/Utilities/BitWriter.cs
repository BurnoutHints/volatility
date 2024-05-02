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
    
    public void Write(uint value, int bitCount)
    {
        for (int i = 0; i < bitCount; i++)
        {
            if ((value & (1 << i)) != 0)
            {
                int byteIndex = currentBit / 8;
                int bitIndex = currentBit % 8;
                buffer[byteIndex] |= (byte)(1 << bitIndex);
            }
            currentBit++;
        }
    }
    
    public byte[] ToArray()
    {
        return buffer;
    }
}
