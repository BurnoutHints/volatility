namespace Volatility.TextureHeader
{
    internal class TextureHeaderPS3 : TextureHeaderBase
    {
        CELL_GCM_TEXTURE_DIMENSION CellDimension;

        public override void PullInternalDimension()
        {
            throw new NotImplementedException();
        }

        public override void PullInternalFlags()
        {
            throw new NotImplementedException();
        }

        public override void PullInternalFormat()
        {
            throw new NotImplementedException();
        }

        public override void PushInternalDimension()
        {
            CELL_GCM_TEXTURE_DIMENSION OutputDimension;
            switch (Dimension)
            {
                case (DIMENSION.DIMENSION_3D):
                    OutputDimension = CELL_GCM_TEXTURE_DIMENSION.CELL_GCM_TEXTURE_DIMENSION_3;
                    break;
                case (DIMENSION.DIMENSION_1D):
                    OutputDimension = CELL_GCM_TEXTURE_DIMENSION.CELL_GCM_TEXTURE_DIMENSION_1;
                    break;
                case (DIMENSION.DIMENSION_CUBE):
                case (DIMENSION.DIMENSION_2D):
                default:
                    OutputDimension = CELL_GCM_TEXTURE_DIMENSION.CELL_GCM_TEXTURE_DIMENSION_2;
                    break;
            }
            CellDimension = OutputDimension;
        }

        public override void PushInternalFlags()
        {
            throw new NotImplementedException();
        }

        public override void PushInternalFormat()
        {
            throw new NotImplementedException();
        }

        public override void WriteToStream(BinaryWriter writer)
        {
            throw new NotImplementedException();
        }
    }

    public enum CELL_GCM_COLOR_FORMAT : int
    {
        CELL_GCM_TEXTURE_B8 = 129,
        CELL_GCM_TEXTURE_A1R5G5B5 = 130,
        CELL_GCM_TEXTURE_A4R4G4B4 = 131,
        CELL_GCM_TEXTURE_R5G6B5 = 132,
        CELL_GCM_TEXTURE_A8R8G8B8 = 133,
        CELL_GCM_TEXTURE_COMPRESSED_DXT1 = 134,
        CELL_GCM_TEXTURE_COMPRESSED_DXT23 = 135,
        CELL_GCM_TEXTURE_COMPRESSED_DXT45 = 136,
        CELL_GCM_TEXTURE_G8B8 = 139,
        CELL_GCM_TEXTURE_R6G5B5 = 143,
        CELL_GCM_TEXTURE_DEPTH24_D8 = 144,
        CELL_GCM_TEXTURE_DEPTH24_D8_FLOAT = 145,
        CELL_GCM_TEXTURE_DEPTH16 = 146,
        CELL_GCM_TEXTURE_DEPTH16_FLOAT = 147,
        CELL_GCM_TEXTURE_X16 = 148,
        CELL_GCM_TEXTURE_Y16_X16 = 149,
        CELL_GCM_TEXTURE_R5G5B5A1 = 151,
        CELL_GCM_TEXTURE_COMPRESSED_HILO8 = 152,
        CELL_GCM_TEXTURE_COMPRESSED_HILO_S8 = 153,
        CELL_GCM_TEXTURE_W16_Z16_Y16_X16_FLOAT = 154,
        CELL_GCM_TEXTURE_W32_Z32_Y32_X32_FLOAT = 155,
        CELL_GCM_TEXTURE_X32_FLOAT = 156,
        CELL_GCM_TEXTURE_D1R5G5B5 = 157,
        CELL_GCM_TEXTURE_D8R8G8B8 = 158,
        CELL_GCM_TEXTURE_Y16_X16_FLOAT = 159,
        CELL_GCM_TEXTURE_COMPRESSED_B8R8_G8R8 = 173,
        CELL_GCM_TEXTURE_COMPRESSED_R8B8_R8G8 = 174
    }

    public enum CELL_GCM_TEXTURE_DIMENSION : byte
    {
        CELL_GCM_TEXTURE_DIMENSION_1 = 1,
        CELL_GCM_TEXTURE_DIMENSION_2 = 2,
        CELL_GCM_TEXTURE_DIMENSION_3 = 3
    }
}
