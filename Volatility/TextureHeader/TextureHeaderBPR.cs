namespace Volatility.TextureHeader
{
    public class TextureHeaderBPR : TextureHeaderBase
    {
        public bool x64Header;                                      // For platforms like PS4

        public readonly ulong TextureInterfacePtr;                  // Set at game runtime, 0 
        public D3D11_USAGE Usage = D3D11_USAGE.D3D11_USAGE_DEFAULT; // Usually default, implemented for parity sake
        public DIMENSION Dimension = DIMENSION.DIMENSION_2D;        // Texture type in TUB
        public ulong PixelDataPtr = 1;                              // Always 1 on PC? Unknown on other platforms
        public readonly ulong ShaderResourceViewInterface0Ptr = 0;  // Set at game runtime, 0 
        public readonly ulong ShaderResourceViewInterface1Ptr = 0;  // Set at game runtime, 0 
        public uint Unknown0 = 0;                                   // Seems to be 0
        public DXGI_FORMAT Format;                                  // Format
        public byte[] Flags = new byte[4];                          // Unknown flags, 0
        public ushort Width;                                        // Width in px
        public ushort Height;                                       // Height in px
        public ushort Depth = 1;                                    // 1 on everything but 3D volume textures
        public ushort ArraySize = 1;                                // Unknown, seems to be 1
        public byte MostDetailedMip;                                // The highest detailed mip to use
        public byte MipLevels;                                      // Amount of mipmaps
        public ushort Unknown1 = 0;                                 // Seems to be 0
        public ulong Unknown2 = 0;                                  // Seems to be 0
        public uint ArrayIndex = 377024;                            // Doc says < 32 but it's always 377024 (C0 C0 05 00)?
        public uint ContentsSize;                                   // PS4/Switch specific field, TODO: Calculate this
        public readonly ulong TextureData;                          // PS4/Switch, Set at game runtime

        public override void PushInternalFormat()
        {
            throw new NotImplementedException();
        }
        public override void PullInternalFormat()
        {
            throw new NotImplementedException();
        }
        public override void PushInternalFlags()
        {
            throw new NotImplementedException();
        }
        public override void PullInternalFlags()
        {
            throw new NotImplementedException();
        }
        public override void WriteToStream(BinaryWriter writer)
        {
            throw new NotImplementedException();
        }
    }
}
