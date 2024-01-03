namespace Volatility.Utilities
{
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
    }
}