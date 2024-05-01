using Volatility.TextureHeader;
using Volatility.Utilities;

namespace Volatility;

internal class AutotestCommand : ICommand
{
    public void Execute()
    {
        /*
         * Right now, the autotest simply creates
         * example texture classes akin to what the parser
         * will interpret from an input format, then write
         * them out to various platform formatted header files.
         */

        // TUB Texture data test case
        TextureHeaderPC textureHeaderPC = new TextureHeaderPC
        {
            Format = D3DFORMAT.D3DFMT_DXT1,
            Width = 1024,
            Height = 512,
            MipLevels = 11,
            GRTexture = true
        };

        WriteTestHeader("autotest_header_PC.dat", textureHeaderPC);

        // BPR Texture data test case
        TextureHeaderBPR textureHeaderBPR = new TextureHeaderBPR
        {
            Format = DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM,
            Width = 1024,
            Height = 512,
            MipLevels = 11,
            GRTexture = true
        };

        // Write 32 bit test BPR header
        WriteTestHeader("autotest_header_BPR.dat", textureHeaderPC);

        textureHeaderBPR.x64Header = true;

        // Write 64 bit test BPR header
        WriteTestHeader("autotest_header_BPRx64.dat", textureHeaderPC);

        // PS3 Texture data test case
        TextureHeaderPS3 textureHeaderPS3 = new TextureHeaderPS3
        {
            Format = CELL_GCM_COLOR_FORMAT.CELL_GCM_TEXTURE_COMPRESSED_DXT45,
            Width = 1024,
            Height = 512,
            MipmapLevels = 11,
            GRTexture = true
        };
        WriteTestHeader("autotest_header_PS3.dat", textureHeaderPS3);


        // File name endian flip test case
        string endianFlipTestName = "12_34_56_78_texture.dat";
        Console.WriteLine($"Flipped endian {endianFlipTestName} to {DataUtilities.FlipFileNameEndian(endianFlipTestName)}");
    }

    public void SetArgs(Dictionary<string, object> args) { }

    public void ShowUsage()
    {
        Console.WriteLine
        (
            "Usage: autotest" +
            "\nRuns a series of automatic tests to ensure the application is working correctly."
        );
    }

    public void WriteTestHeader(string name, TextureHeaderBase header) 
    {
        using (FileStream fs = new FileStream(name, FileMode.Create))
        using (BinaryWriter writer = new BinaryWriter(fs))
        {
            header.WriteToStream(writer);
            writer.Close();
            fs.Close();
        }
    }
}