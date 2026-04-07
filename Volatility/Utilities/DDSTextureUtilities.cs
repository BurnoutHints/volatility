namespace Volatility.Utilities;

public static class DDSTextureUtilities
{
    public static void A8R8G8B8toB8G8R8A8(byte[] pixelData, int width, int height, int mipmapCount)
    {
        int offset = 0;
        for (int mip = 0; mip < mipmapCount; mip++)
        {
            int mipWidth = Math.Max(1, width >> mip);
            int mipHeight = Math.Max(1, height >> mip);
            int mipSize = mipWidth * mipHeight * 4;

            for (int i = offset; i < offset + mipSize; i += 4)
            {
                byte alpha = pixelData[i];
                byte red = pixelData[i + 1];
                byte green = pixelData[i + 2];
                byte blue = pixelData[i + 3];

                pixelData[i] = blue;
                pixelData[i + 1] = green;
                pixelData[i + 2] = red;
                pixelData[i + 3] = alpha;
            }

            offset += mipSize;
        }
    }

    public static void A8R8G8B8toR8G8B8A8(byte[] pixelData, int width, int height, int mipmapCount)
    {
        int offset = 0;
        for (int mip = 0; mip < mipmapCount; mip++)
        {
            int mipWidth = Math.Max(1, width >> mip);
            int mipHeight = Math.Max(1, height >> mip);
            int mipSize = mipWidth * mipHeight * 4;

            for (int i = offset; i < offset + mipSize; i += 4)
            {
                byte alpha = pixelData[i];
                byte red = pixelData[i + 1];
                byte green = pixelData[i + 2];
                byte blue = pixelData[i + 3];

                pixelData[i] = red;
                pixelData[i + 1] = green;
                pixelData[i + 2] = blue;
                pixelData[i + 3] = alpha;
            }

            offset += mipSize;
        }
    }

    public static void A8R8G8B8toA8B8G8R8(byte[] pixelData, int width, int height, int mipmapCount)
    {
        int offset = 0;
        for (int mip = 0; mip < mipmapCount; mip++)
        {
            int mipWidth = Math.Max(1, width >> mip);
            int mipHeight = Math.Max(1, height >> mip);
            int mipSize = mipWidth * mipHeight * 4;

            for (int i = offset; i < offset + mipSize; i += 4)
            {
                byte alpha = pixelData[i];
                byte red = pixelData[i + 1];
                byte green = pixelData[i + 2];
                byte blue = pixelData[i + 3];

                pixelData[i] = alpha;
                pixelData[i + 1] = blue;
                pixelData[i + 2] = green;
                pixelData[i + 3] = red;
            }

            offset += mipSize;
        }
    }

    public static void R8G8B8A8toA8R8G8B8(byte[] pixelData, int width, int height, int mipmapCount)
    {
        int offset = 0;
        for (int mip = 0; mip < mipmapCount; mip++)
        {
            int mipWidth = Math.Max(1, width >> mip);
            int mipHeight = Math.Max(1, height >> mip);
            int mipSize = mipWidth * mipHeight * 4;

            for (int i = offset; i < offset + mipSize; i += 4)
            {
                byte red = pixelData[i];
                byte green = pixelData[i + 1];
                byte blue = pixelData[i + 2];
                byte alpha = pixelData[i + 3];

                pixelData[i] = alpha;
                pixelData[i + 1] = red;
                pixelData[i + 2] = green;
                pixelData[i + 3] = blue;
            }

            offset += mipSize;
        }
    }

    public static void B8G8R8A8toA8R8G8B8(byte[] pixelData, int width, int height, int mipmapCount)
    {
        int offset = 0;
        for (int mip = 0; mip < mipmapCount; mip++)
        {
            int mipWidth = Math.Max(1, width >> mip);
            int mipHeight = Math.Max(1, height >> mip);
            int mipSize = mipWidth * mipHeight * 4;

            for (int i = offset; i < offset + mipSize; i += 4)
            {
                byte blue = pixelData[i];
                byte green = pixelData[i + 1];
                byte red = pixelData[i + 2];
                byte alpha = pixelData[i + 3];

                pixelData[i] = alpha;
                pixelData[i + 1] = red;
                pixelData[i + 2] = green;
                pixelData[i + 3] = blue;
            }

            offset += mipSize;
        }
    }

    public static void A8B8G8R8toA8R8G8B8(byte[] pixelData, int width, int height, int mipmapCount)
    {
        int offset = 0;
        for (int mip = 0; mip < mipmapCount; mip++)
        {
            int mipWidth = Math.Max(1, width >> mip);
            int mipHeight = Math.Max(1, height >> mip);
            int mipSize = mipWidth * mipHeight * 4;

            for (int i = offset; i < offset + mipSize; i += 4)
            {
                byte alpha = pixelData[i];
                byte blue = pixelData[i + 1];
                byte green = pixelData[i + 2];
                byte red = pixelData[i + 3];

                pixelData[i] = alpha;
                pixelData[i + 1] = red;
                pixelData[i + 2] = green;
                pixelData[i + 3] = blue;
            }

            offset += mipSize;
        }
    }

    public static void A8B8G8R8toB8G8R8A8(byte[] pixelData, int width, int height, int mipmapCount)
    {
        int offset = 0;
        for (int mip = 0; mip < mipmapCount; mip++)
        {
            int mipWidth = Math.Max(1, width >> mip);
            int mipHeight = Math.Max(1, height >> mip);
            int mipSize = mipWidth * mipHeight * 4;

            for (int i = offset; i < offset + mipSize; i += 4)
            {
                byte alpha = pixelData[i];
                byte blue = pixelData[i + 1];
                byte green = pixelData[i + 2];
                byte red = pixelData[i + 3];

                pixelData[i] = blue;
                pixelData[i + 1] = green;
                pixelData[i + 2] = red;
                pixelData[i + 3] = alpha;
            }

            offset += mipSize;
        }
    }

}
