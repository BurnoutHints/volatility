using Volatility.TextureHeader;
using Volatility.Utilities;

internal class Frontend
{
    private static void Main(string[] args)
    {
        /*
         * Right now, the application simply creates
         * example texture classes akin to what the parser
         * will interpret from an input format, then write
         * them out to various platform formatted header files.
         * 
         * ! = Completed
         * ? = WIP/Needs Testing
         * > = To Do
         * 
         * DONE!
         * --------------------------
         * ! Bit accurate PC header
         * 
         * TODO LIST!
         * --------------------------
         * ? Bit accurate BPR (PC) header (needs testing)
         * ? Proper x64 header export size (need x64 file reference)
         * > User input (CLI/GUI?)
         * > Header parsing logic
         * > PS3/X360 header formats
         * 
         * LOW PRIORITY
         * --------------------------
         * ? Bit accurate BPR headers for other/x64 platforms
         * > Raw DDS texture importing (bundle manager does this)
         * 
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

        // Write example texture data to file
        using (FileStream fs = new FileStream("test_header_PC.dat", FileMode.Create))
        using (BinaryWriter writer = new BinaryWriter(fs))
        {
            textureHeaderPC.WriteToStream(writer);
            writer.Close();
            fs.Close();
        }

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
        using (FileStream fs = new FileStream("test_header_BPR.dat", FileMode.Create))
        using (BinaryWriter writer = new BinaryWriter(fs))
        {
            textureHeaderBPR.WriteToStream(writer);
            writer.Close();
            fs.Close();
        }

        textureHeaderBPR.x64Header = true;

        // Write 64 bit test BPR header
        using (FileStream fs = new FileStream("test_header_BPRx64.dat", FileMode.Create))
        using (BinaryWriter writer = new BinaryWriter(fs))
        {
            textureHeaderBPR.WriteToStream(writer);
            writer.Close();
            fs.Close();
        }

        // File name endian flip test case
        string endianFlipTestName = "12_34_56_78_texture.dat";
        Console.WriteLine($"Flipped endian {endianFlipTestName} to {DataUtilities.FlipFileNameEndian(endianFlipTestName)}");
    }
}