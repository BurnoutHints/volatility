﻿using Volatility.TextureHeader;

internal class Frontend
{
    private static void Main(string[] args)
    {
        /*
         * Right now, the application simply creates a
         * texture class akin to what the parser will 
         * interpret from an input format, then write
         * it out to a PC (TUB) formatted header file.
         * 
         * ! = Completed
         * ? = WIP/Needs Testing
         * > = To Do
         * 
         * TODO LIST!
         * --------------------------
         * ! Bit accurate PC header
         * ? Bit accurate BPR (PC) header (needs testing)
         * > Header parsing logic
         * 
         * LOW PRIORITY
         * --------------------------
         * ? Bit accurate BPR headers for other/x64 platforms
         * > Raw DDS texture importing (bundle manager does this)
         * > PS3/X360 header formats?
         * 
         */

        // Example Texture Data
        TextureHeaderPC textureHeaderPC = new TextureHeaderPC
        {
            Format = D3DFORMAT.D3DFMT_DXT1,
            Width = 1024,
            Height = 512,
            MipLevels = 11,
            GRTexture = true
        };

        // Write example texture data to file
        using (FileStream fs = new FileStream("texture_header.bin", FileMode.Create))
        using (BinaryWriter writer = new BinaryWriter(fs))
        {
            textureHeaderPC.WriteToStream(writer);
        }
    }
}