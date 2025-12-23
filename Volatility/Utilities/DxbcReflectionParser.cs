using System.Buffers.Binary;
using System.Text;

namespace Volatility.Utilities;

public static class DxbcReflectionParser
{
    public static DxbcReflectionData Parse(byte[] csoBytes)
    {
        if (csoBytes == null || csoBytes.Length < 32)
            throw new InvalidOperationException("CSO data is empty or too small.");

        if (!TryGetChunk(csoBytes, "RDEF", out var chunkData))
            throw new InvalidOperationException("RDEF chunk not found in CSO.");

        return ParseRdef(chunkData);
    }

    private static DxbcReflectionData ParseRdef(ReadOnlySpan<byte> data)
    {
        if (data.Length < 16)
            return new DxbcReflectionData();

        uint constantBufferCount = ReadUInt32(data, 0);
        uint boundResourceCount = ReadUInt32(data, 4);
        uint constantBufferOffset = ReadUInt32(data, 8);
        uint boundResourceOffset = ReadUInt32(data, 12);

        var reflection = new DxbcReflectionData();

        ParseConstantBuffers(data, reflection, constantBufferCount, constantBufferOffset);
        ParseResourceBindings(data, reflection, boundResourceCount, boundResourceOffset);

        return reflection;
    }

    private static void ParseConstantBuffers(ReadOnlySpan<byte> data, DxbcReflectionData reflection, uint count, uint offset)
    {
        const int cbufferStride = 24;
        const int variableStride = 40;

        for (int i = 0; i < count; i++)
        {
            int descOffset = (int)offset + i * cbufferStride;
            if (descOffset + cbufferStride > data.Length)
                break;

            uint nameOffset = ReadUInt32(data, descOffset + 0);
            uint variableCount = ReadUInt32(data, descOffset + 4);
            uint variableOffset = ReadUInt32(data, descOffset + 8);
            uint size = ReadUInt32(data, descOffset + 12);

            string name = ReadString(data, (int)nameOffset);
            var buffer = new DxbcConstantBuffer
            {
                Name = name,
                Size = size
            };

            for (int v = 0; v < variableCount; v++)
            {
                int varOffset = (int)variableOffset + v * variableStride;
                if (varOffset + variableStride > data.Length)
                    break;

                uint varNameOffset = ReadUInt32(data, varOffset + 0);
                uint startOffset = ReadUInt32(data, varOffset + 4);
                uint varSize = ReadUInt32(data, varOffset + 8);
                uint typeOffset = ReadUInt32(data, varOffset + 16);

                string varName = ReadString(data, (int)varNameOffset);
                DxbcTypeDesc typeDesc = ReadTypeDesc(data, (int)typeOffset);

                buffer.Variables.Add(new DxbcConstantVariable
                {
                    Name = varName,
                    StartOffset = startOffset,
                    Size = varSize,
                    TypeDesc = typeDesc
                });
            }

            reflection.ConstantBuffers.Add(buffer);
        }
    }

    private static void ParseResourceBindings(ReadOnlySpan<byte> data, DxbcReflectionData reflection, uint count, uint offset)
    {
        int stride = DetermineResourceStride(data, count, offset);

        for (int i = 0; i < count; i++)
        {
            int descOffset = (int)offset + i * stride;
            if (descOffset + stride > data.Length)
                break;

            uint nameOffset = ReadUInt32(data, descOffset + 0);
            uint type = ReadUInt32(data, descOffset + 4);
            uint bindPoint = ReadUInt32(data, descOffset + 8);
            uint bindCount = ReadUInt32(data, descOffset + 12);

            string name = ReadString(data, (int)nameOffset);
            reflection.ResourceBindings.Add(new DxbcResourceBinding
            {
                Name = name,
                ResourceType = (DxbcResourceType)type,
                BindPoint = bindPoint,
                BindCount = bindCount
            });
        }
    }

    private static int DetermineResourceStride(ReadOnlySpan<byte> data, uint count, uint offset)
    {
        const int stride32 = 32;
        const int stride40 = 40;

        int valid32 = CountValidResourceNames(data, count, offset, stride32);
        int valid40 = CountValidResourceNames(data, count, offset, stride40);

        return valid40 > valid32 ? stride40 : stride32;
    }

    private static int CountValidResourceNames(ReadOnlySpan<byte> data, uint count, uint offset, int stride)
    {
        int valid = 0;
        for (int i = 0; i < count; i++)
        {
            int descOffset = (int)offset + i * stride;
            if (descOffset + 4 > data.Length)
                break;

            uint nameOffset = ReadUInt32(data, descOffset + 0);
            if (nameOffset >= data.Length)
                continue;

            string name = ReadString(data, (int)nameOffset);
            if (!string.IsNullOrEmpty(name))
                valid++;
        }

        return valid;
    }

    private static DxbcTypeDesc ReadTypeDesc(ReadOnlySpan<byte> data, int offset)
    {
        const int typeStride = 32;
        if (offset < 0 || offset + typeStride > data.Length)
            return new DxbcTypeDesc();

        uint rows = ReadUInt32(data, offset + 8);
        uint columns = ReadUInt32(data, offset + 12);
        uint elements = ReadUInt32(data, offset + 16);

        return new DxbcTypeDesc
        {
            Rows = rows,
            Columns = columns,
            Elements = elements
        };
    }

    private static bool TryGetChunk(byte[] data, string fourCc, out ReadOnlySpan<byte> chunk)
    {
        chunk = default;
        if (data.Length < 32)
            return false;

        if (Encoding.ASCII.GetString(data, 0, 4) != "DXBC")
            return false;

        uint chunkCount = ReadUInt32(data, 0x1C);
        int chunkTableOffset = 0x20;

        for (int i = 0; i < chunkCount; i++)
        {
            int offset = (int)ReadUInt32(data, chunkTableOffset + i * 4);
            if (offset + 8 > data.Length)
                continue;

            string tag = Encoding.ASCII.GetString(data, offset, 4);
            uint size = ReadUInt32(data, offset + 4);
            int dataOffset = offset + 8;

            if (tag == fourCc && dataOffset + size <= data.Length)
            {
                chunk = new ReadOnlySpan<byte>(data, dataOffset, (int)size);
                return true;
            }
        }

        return false;
    }

    private static uint ReadUInt32(ReadOnlySpan<byte> data, int offset)
    {
        return BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(offset, 4));
    }

    private static string ReadString(ReadOnlySpan<byte> data, int offset)
    {
        if (offset < 0 || offset >= data.Length)
            return string.Empty;

        int end = offset;
        while (end < data.Length && data[end] != 0)
            end++;

        if (end <= offset)
            return string.Empty;

        return Encoding.ASCII.GetString(data.Slice(offset, end - offset));
    }
}

public sealed class DxbcReflectionData
{
    public List<DxbcConstantBuffer> ConstantBuffers { get; } = [];
    public List<DxbcResourceBinding> ResourceBindings { get; } = [];
}

public sealed class DxbcConstantBuffer
{
    public string Name { get; set; } = string.Empty;
    public uint Size { get; set; }
    public List<DxbcConstantVariable> Variables { get; } = [];
}

public sealed class DxbcConstantVariable
{
    public string Name { get; set; } = string.Empty;
    public uint StartOffset { get; set; }
    public uint Size { get; set; }
    public DxbcTypeDesc TypeDesc { get; set; } = new();
}

public sealed class DxbcResourceBinding
{
    public string Name { get; set; } = string.Empty;
    public DxbcResourceType ResourceType { get; set; }
    public uint BindPoint { get; set; }
    public uint BindCount { get; set; }
}

public enum DxbcResourceType : uint
{
    CBuffer = 0,
    TBuffer = 1,
    Texture = 2,
    Sampler = 3,
    UavRwTyped = 4,
    Structured = 5,
    UavRwStructured = 6,
    ByteAddress = 7,
    UavRwByteAddress = 8,
    UavAppendStructured = 9,
    UavConsumeStructured = 10,
    UavRwStructuredWithCounter = 11,
    UavFeedbackTexture = 12,
    UavRwFeedbackTexture = 13
}

public sealed class DxbcTypeDesc
{
    public uint Rows { get; set; } = 1;
    public uint Columns { get; set; } = 1;
    public uint Elements { get; set; }
}
