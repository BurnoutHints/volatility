namespace Volatility.TextureHeader
{
    public abstract class TextureHeaderBase
    {
        protected bool _GRTexture = false;
        public bool GRTexture       // Vehicle & Wheel GRs 
        {
            get => _GRTexture;
            set 
            {
                _GRTexture = value;
                PushInternalFlags(); 
            }
        }

        protected bool _WorldTexture = false;
        public bool WorldTexture    // GlobalBackdrops & WorldTex
        {
            get => _WorldTexture;
            set 
            {
                _WorldTexture = value;
                PushInternalFlags(); 
            }
        }

        protected bool _PropTexture = false;
        public bool PropTexture     // GlobalProps
        {
            get => _PropTexture;
            set 
            {
                _PropTexture = value;
                PushInternalFlags(); 
            }
        }

        protected DIMENSION _Dimension = DIMENSION.DIMENSION_2D;
        public virtual DIMENSION Dimension // GPUDIMENSION / renderengine::Texture::Type
        {
            get => _Dimension;
            set
            {
                _Dimension = value;
                PushInternalDimension();
            }
        }

        public abstract void PushInternalDimension();
        public abstract void PullInternalDimension();

        public abstract void PushInternalFormat();
        public abstract void PullInternalFormat();

        public abstract void PushInternalFlags();
        public abstract void PullInternalFlags();

        public abstract void WriteToStream(BinaryWriter writer);
        public abstract void ParseFromStream(BinaryReader reader);
    }

    // BPR formatted but converted for each platform
    public enum DIMENSION : int
    {
        DIMENSION_1D = 6,
        DIMENSION_2D = 7,
        DIMENSION_3D = 8,
        DIMENSION_CUBE = 9
    }
}
