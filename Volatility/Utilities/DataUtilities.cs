namespace Volatility.Utilities
{
    public static class DataUtilities
    {
        public static byte TrimIntToByte(int input)
        {
            return BitConverter.GetBytes(input)[0];
        }
    }
}