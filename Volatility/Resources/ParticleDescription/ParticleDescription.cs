using System;
using System.Runtime.InteropServices;

namespace Volatility.Resources;

public class ParticleDescription : Resource
{
    public override ResourceType GetResourceType() => ResourceType.ParticleDescription;
    public override Platform GetResourcePlatform() => Platform.Agnostic;

    public override void WriteToStream(EndianAwareBinaryWriter writer, Endian endianness = Endian.Agnostic)
    {
        base.WriteToStream(writer);
    }
    public override void ParseFromStream(ResourceBinaryReader reader, Endian endianness = Endian.Agnostic)
    {
        base.ParseFromStream(reader);
    }

    public ParticleDescription() : base() { }

    public ParticleDescription(string path, Endian endianness = Endian.Agnostic) : base(path, endianness) { }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct cLionEffectDefinition
    {
        public uint mVersion;
        public uint m_key;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public ushort[] m_name;
        public IntPtr mpParticles;
        public IntPtr mpBindings;
        public IntPtr mpNext;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct cLionParticleEffect
    {
        public uint mHash;
        public IntPtr mpDescriptors;
        public IntPtr mpNext;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct cParticleDescriptor
    {
        public uint mID;
        public float mPauseTime, mPauseTimeVariance;
        public float mRepeatTime, mRepeatTimeVariance;
        public float mEmitterLifeBase, mEmitterLifeVariance;
        public uint mEmitterLifeInfiniteFlag;
        public uint mFlags, mLodGroup, mRenderGroup, mShape, mCollisionType;
        public float mBlendLast;
        public IntPtr mpName;
        public int mBehaviourCount;
        public IntPtr mpBehaviours, mpBehaviourTemp, mpBehaviour;
        public IntPtr mpMaterial;
        public IntPtr mpDef;
        public IntPtr mpNext, mpParent, mpChild;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct cParticleMaterial
    {
        public uint mID, mMaterialHandle, mMeshHandle, mTextureHandle;
        public IntPtr mpTextureName;
        public uint mNormalMapHandle;
        public IntPtr mpNormalMapName, mpMeshName, mpLayerGroupName;
        public uint mFlags, mFrameMask;
        public int mFrameBase, mFrameVariance, mFrameCount;
        public byte mXFrames, mYFrames, mBlendMode, mAlphaTestMode;
        public byte mAlphaTestValue, mZTestMode, mPad, mUCoordOption;
        public byte mVCoordOption, mAnimTexOptions, mShader, mNormalOption;
        public uint mLayer;
        public float mRibbonStretch;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public uint[] mMeshHandles;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public IntPtr[] mpMeshNames;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public uint[] mPercentages;
        public uint mNumMeshes;
        public float mNormalBlend, mKeyLightAmount, mIBLAmount, mZBlendDistance;
        public float mFPS, mFPSVariance;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct cLionBindings
    {
        public uint mLocatorCount, mWorldIndex;
        public IntPtr mppLocators, mpLocator, mpScaler, mpTrigger;
        private ulong _pad0;
        public cParticleRandomSeed mSeed;
        public IntPtr mpNext, m_p_emitter;
        private ulong _pad1;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct cParticleRandomSeed
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 48)]
        public byte[] mCgsRandom;
        public uint mSeed;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
        public byte[] pad;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct sParticleNucleus
    {
        public cVector mPos, mVel, mAcc, mRot, mRotVel, mRotAcc;
        public cVector mOffsetRot, mOffsetRotVel, mOffsetRotAcc;
        public cVector mSize, mSizeVel, mSizeAcc, mLocatorVel;
        public cVector mvLifeTimeAndFrameTimeAndFPSAndBirthTime;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct cParticleBucket
    {
        public IntPtr mpManagerNext;
        public IntPtr mpEmitter;
        public IntPtr mpEmitterNext;
        public cTime mLatestBirthTime;
        public cParticleRandomSeed mRandomSeed;
        public uint mnNextParticlePositionToFill;
        public uint mActiveBits;
        private ulong _pad0;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public sParticleNucleus[] mParticles;
        public IntPtr mpMatrices;
        public IntPtr mpVectors;
        private ulong _pad1;
    }

}
