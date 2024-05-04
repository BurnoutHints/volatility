using Volatility.TextureHeader;

namespace Volatility.Utilities
{
    internal class X360TextureUtilities
    {
        public static void WriteUntiled360TextureFile(TextureHeaderX360 xboxHeader, string textureBitmapPath, string outPath = "")
        {
            if (string.IsNullOrEmpty(outPath))
            {
                outPath = textureBitmapPath;
            }
            if (xboxHeader.Format.Tiled)
            {
                FileStream stream = new FileStream(textureBitmapPath, FileMode.Open, FileAccess.Read);
                byte[] bitmapData = new byte[stream.Length];
                
                stream.Read(bitmapData);
                bitmapData = ConvertToLinearTexture(bitmapData, (int)xboxHeader.Format.Size.Width, (int)xboxHeader.Format.Size.Height, xboxHeader.Format.DataFormat);
                stream.Close();
                
                stream = new FileStream(outPath, FileMode.OpenOrCreate, FileAccess.Write);
                stream.Write(bitmapData);
                stream.Close();
            }
        }

        // THE BELOW CODE IS CREDITED TO NCDyson for RareView
        // AND "Pimpin Tyler and Anthony" for GTA IV Xbox 360 Texture Editor

        // It originated from the GTA IV Xbox 360 Texture Editor,
        // in which its source code was released publicly.

        // I borrowed it from RareView as links to the GTA version seem to be dead.

        // THERE WAS NO PROPER LICENSE FOR IT THOUGH, I DID NOT WRITE IT!!

        // Its calculations are accurate to what the Xbox 360 does officially,
        // so the output would be identical whether I spent the insane amount
        // of hours figuring it out and writing it opposed to not reinventing the wheel.

        private static byte[] ConvertToLinearTexture(byte[] data, int _width, int _height, GPUTEXTUREFORMAT _textureFormat)
        {
            byte[] destData = new byte[data.Length];

            int blockSize;
            int texelPitch;

            switch (_textureFormat)
            {
                case GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_8_8:
                    blockSize = 1;
                    texelPitch = 2;
                    break;
                case GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_8:
                    blockSize = 1;
                    texelPitch = 1;
                    break;
                case GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT1:
                    blockSize = 4;
                    texelPitch = 8;
                    break;
                case GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT2_3:
                case GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT4_5:
                case GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXN:
                    blockSize = 4;
                    texelPitch = 16;
                    break;
                case GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_8_8_8_8:
                    blockSize = 1;
                    texelPitch = 4;
                    break;
                case GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_4_4_4_4:
                    blockSize = 1;
                    texelPitch = 2;
                    break;
                case GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_5_6_5:
                    blockSize = 1;
                    texelPitch = 2;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("Bad texture type!");
            }

            int blockWidth = _width / blockSize;
            int blockHeight = _height / blockSize;

            for (int j = 0; j < blockHeight; j++)
            {
                for (int i = 0; i < blockWidth; i++)
                {
                    int blockOffset = j * blockWidth + i;

                    int x = XGAddress2DTiledX(blockOffset, blockWidth, texelPitch);
                    int y = XGAddress2DTiledY(blockOffset, blockWidth, texelPitch);

                    int srcOffset = j * blockWidth * texelPitch + i * texelPitch;
                    int destOffset = y * blockWidth * texelPitch + x * texelPitch;
                    //TODO: ConvertToLinearTexture apparently breaks on on textures with a height of 64...
                    if (destOffset >= destData.Length) continue;
                    Array.Copy(data, srcOffset, destData, destOffset, texelPitch);
                }
            }

            return destData;
        }

        private static int XGAddress2DTiledX(int Offset, int Width, int TexelPitch)
        {
            int AlignedWidth = (Width + 31) & ~31;

            int LogBpp = (TexelPitch >> 2) + ((TexelPitch >> 1) >> (TexelPitch >> 2));
            int OffsetB = Offset << LogBpp;
            int OffsetT = ((OffsetB & ~4095) >> 3) + ((OffsetB & 1792) >> 2) + (OffsetB & 63);
            int OffsetM = OffsetT >> (7 + LogBpp);

            int MacroX = ((OffsetM % (AlignedWidth >> 5)) << 2);
            int Tile = ((((OffsetT >> (5 + LogBpp)) & 2) + (OffsetB >> 6)) & 3);
            int Macro = (MacroX + Tile) << 3;
            int Micro = ((((OffsetT >> 1) & ~15) + (OffsetT & 15)) & ((TexelPitch << 3) - 1)) >> LogBpp;

            return Macro + Micro;
        }

        private static int XGAddress2DTiledY(int Offset, int Width, int TexelPitch)
        {
            int AlignedWidth = (Width + 31) & ~31;

            int LogBpp = (TexelPitch >> 2) + ((TexelPitch >> 1) >> (TexelPitch >> 2));
            int OffsetB = Offset << LogBpp;
            int OffsetT = ((OffsetB & ~4095) >> 3) + ((OffsetB & 1792) >> 2) + (OffsetB & 63);
            int OffsetM = OffsetT >> (7 + LogBpp);

            int MacroY = ((OffsetM / (AlignedWidth >> 5)) << 2);
            int Tile = ((OffsetT >> (6 + LogBpp)) & 1) + (((OffsetB & 2048) >> 10));
            int Macro = (MacroY + Tile) << 3;
            int Micro = ((((OffsetT & (((TexelPitch << 6) - 1) & ~31)) + ((OffsetT & 15) << 1)) >> (3 + LogBpp)) & ~1);

            return Macro + Micro + ((OffsetT & 16) >> 4);
        }
    }
}
