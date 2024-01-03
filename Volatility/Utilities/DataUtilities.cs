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
            return BitConverter.GetBytes(x64 ? value : (uint)value);
        }
    }
}