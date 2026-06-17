using Volatility.Resources;

namespace Volatility.Abstractions.Services;

public interface ITextureBitmapStore
{
    string GetResourceBaseName(string headerPath, Unpacker unpacker);
    string GetSecondaryBitmapPath(string headerPath, Unpacker unpacker);
    byte[] ReadNormalizedBitmapData(TextureBase texture, string bitmapPath);
    void WriteNormalizedBitmapFile(TextureBase texture, string sourceBitmapPath, string outputPath, bool overwrite = true);
    void ConvertPS3GTFToDDS(TexturePS3 texture, string sourceBitmapPath, string destinationBitmapPath, bool verbose = false);
}
