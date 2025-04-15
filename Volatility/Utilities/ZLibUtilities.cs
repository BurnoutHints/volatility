using System.IO;
using System.IO.Compression;

public static class ZLibUtilities
{
    public static byte[] Compress(byte[] rawData)
    {
        using (var outputStream = new MemoryStream())
        {
            using (var zlibStream = new ZLibStream(outputStream, CompressionLevel.Optimal))
            {
                zlibStream.Write(rawData, 0, rawData.Length);
            }
            return outputStream.ToArray();
        }
    }

    public static byte[] Decompress(byte[] compressedData)
    {
        using (var inputStream = new MemoryStream(compressedData))
        using (var zlibStream = new ZLibStream(inputStream, CompressionMode.Decompress))
        using (var outputStream = new MemoryStream())
        {
            zlibStream.CopyTo(outputStream);
            return outputStream.ToArray();
        }
    }
}