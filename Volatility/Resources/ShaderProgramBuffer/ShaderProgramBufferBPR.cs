using static Volatility.Utilities.DataUtilities;

using System.Text;

using Volatility.Utilities;

namespace Volatility.Resources;

public class ShaderProgramBufferBPR : ShaderProgramBufferBase
{
    public override Endian GetResourceEndian() => Endian.LE;
    public override Platform GetResourcePlatform() => Platform.BPR;

    public ShaderType CompiledShaderType;

    public uint CompiledShaderSize;

    public byte[] CompiledShaderBytecode = [];

    public List<ShaderProgramBinding> Bindings = [];

    public List<RegisterComponent> Globals = [];

    public override void WriteToStream(EndianAwareBinaryWriter writer, Endian endianness = Endian.Agnostic)
    {
        base.WriteToStream(writer, endianness);

        bool hasGlobals = Globals.Count > 0;
        byte[] compiledShader = CompiledShaderBytecode ?? [];
        CompiledShaderSize = (uint)compiledShader.Length;

        int headerSize = 0x54;
        int bindingStride = 0x34;
        int bindingsSize = Bindings.Count * bindingStride;
        int compiledShaderOffset = headerSize + bindingsSize;
        int registerComponentsSize = Globals.Count * 0x10;
        int registerComponentsOffset = compiledShaderOffset + (int)CompiledShaderSize;
        int stringTableOffset = registerComponentsOffset + registerComponentsSize;

        BuildStringTable(stringTableOffset, out var bindingNameOffsets, out var globalNameOffsets, out uint globalsStringOffset, out byte[] stringTable);

        writer.Write((uint)CompiledShaderType); // Type
        writer.Write(0u); // Unknown (runtime)
        writer.Write(0u); // Runtime ptr
        writer.Write(hasGlobals ? 1u : 0u); // Uses globals?
        writer.Write(0u); // Unknown
        writer.Write(CompiledShaderSize);
        writer.Write((uint)stringTable.Length);
        writer.Write((ushort)Bindings.Count);
        writer.Write(new byte[0x32]); // Mask / padding
        writer.Write(0u); // Runtime ptr

        for (int i = 0; i < Bindings.Count; i++)
        {
            ShaderProgramBinding binding = Bindings[i];
            writer.Write(bindingNameOffsets[i]);
            writer.Write(binding.Index);
            writer.Write((byte)binding.BindingType);
            writer.Write(binding.Unknown0);
            writer.Write(binding.Unknown1);

            // Reserved / unused block (0x08 - 0x17)
            writer.Write(0u);
            writer.Write(0u);
            writer.Write(0u);
            writer.Write(0u);

            if (binding.BindingType == ShaderBindingType.Globals)
            {
                writer.Write(0u);
                writer.Write(0u);
                writer.Write(0u);
                writer.Write(globalsStringOffset);
                writer.Write((uint)Globals.Count);
                writer.Write(0u);
                writer.Write((uint)registerComponentsOffset);
            }
            else
            {
                writer.Write(0u);
                writer.Write(0u);
                writer.Write(0u);
                writer.Write(0u);
                writer.Write(0u);
                writer.Write(0u);
                writer.Write(0u);
            }
        }

        if (compiledShader.Length > 0)
        {
            writer.Write(compiledShader);
        }

        for (int i = 0; i < Globals.Count; i++)
        {
            RegisterComponent component = Globals[i];
            writer.Write(globalNameOffsets[i]);
            writer.Write(component.BufferOffset);
            writer.Write(component.BufferSize);
            writer.Write(component.Id);
            writer.Write(component.Type);
            writer.Write(component.RegisterCount);
            writer.Write(component.ComponentCount);
        }

        if (stringTable.Length > 0)
        {
            writer.Write(stringTable);
        }
    }
    public override void ParseFromStream(ResourceBinaryReader reader, Endian endianness = Endian.Agnostic)
    {
        base.ParseFromStream(reader, endianness);
        
        CompiledShaderType = (ShaderType)reader.ReadUInt32();
        
        reader.BaseStream.Seek(0x14, SeekOrigin.Begin);
        CompiledShaderSize = reader.ReadUInt32();

        reader.BaseStream.Seek(0x1C, SeekOrigin.Begin);
        ushort bindingCount = reader.ReadUInt16();

        long compiledShaderOffset = 0x54 + (bindingCount * 0x34);
        if (CompiledShaderSize > 0
            && CompiledShaderSize <= int.MaxValue
            && compiledShaderOffset + CompiledShaderSize <= reader.BaseStream.Length)
        {
            reader.BaseStream.Seek(compiledShaderOffset, SeekOrigin.Begin);
            CompiledShaderBytecode = reader.ReadBytes((int)CompiledShaderSize);
        }
        else
        {
            CompiledShaderBytecode = [];
        }
    }

    public ShaderProgramBufferBPR() : base() { }

    public ShaderProgramBufferBPR(string path) : base(path) { }

    public static ShaderProgramBufferBPR FromCSO(byte[] csoBytes, ShaderStageType stage)
    {
        if (csoBytes == null)
            throw new ArgumentNullException(nameof(csoBytes));

        var reflection = DxbcReflectionParser.Parse(csoBytes);

        var buffer = new ShaderProgramBufferBPR
        {
            CompiledShaderType = ToShaderType(stage),
            CompiledShaderBytecode = csoBytes
        };

        foreach (var resource in reflection.ResourceBindings)
        {
            ShaderBindingType? bindingType = resource.ResourceType switch
            {
                DxbcResourceType.Sampler => ShaderBindingType.Sampler,
                DxbcResourceType.Texture => ShaderBindingType.Texture,
                _ => null
            };

            if (bindingType == null)
                continue;

            buffer.Bindings.Add(new ShaderProgramBinding
            {
                Name = resource.Name,
                Index = ClampToByte(resource.BindPoint),
                BindingType = bindingType.Value
            });
        }

        var globalsBuffer = reflection.ConstantBuffers.FirstOrDefault(cb => cb.Name == "$Globals")
            ?? reflection.ConstantBuffers.FirstOrDefault();

        if (globalsBuffer != null && globalsBuffer.Variables.Count > 0)
        {
            foreach (var variable in globalsBuffer.Variables.OrderBy(variable => variable.StartOffset))
            {
                byte registerCount = CalculateRegisterCount(variable.Size);
                byte componentCount = CalculateComponentCount(variable.TypeDesc);
                uint bufferSize = (uint)registerCount * 16u;

                buffer.Globals.Add(new RegisterComponent
                {
                    Name = variable.Name,
                    BufferOffset = variable.StartOffset,
                    BufferSize = bufferSize,
                    Id = 0,
                    Type = 0x03,
                    RegisterCount = registerCount,
                    ComponentCount = componentCount
                });
            }

            buffer.Bindings.Add(new ShaderProgramBinding
            {
                Name = "$Globals",
                Index = 0,
                BindingType = ShaderBindingType.Globals
            });
        }

        return buffer;
    }

    public enum ShaderType : uint
    {
        VertexShader,
        PixelShader
    };

    public enum ShaderBindingType : byte
    {
        Sampler = 0x05,
        Texture = 0x0A,
        Globals = 0x1A
    }

    public sealed class ShaderProgramBinding
    {
        public string Name = string.Empty;
        public byte Index;
        public ShaderBindingType BindingType;
        public byte Unknown0 = 1;
        public byte Unknown1 = 0;
    }

    public sealed class RegisterComponent
    {
        public string Name = string.Empty;
        public uint BufferOffset;
        public uint BufferSize;
        public byte Id;
        public byte Type;
        public byte RegisterCount;
        public byte ComponentCount;
    }

    private void BuildStringTable(
        int stringTableOffset,
        out List<uint> bindingNameOffsets,
        out List<uint> globalNameOffsets,
        out uint globalsStringOffset,
        out byte[] stringTable)
    {
        bindingNameOffsets = new List<uint>(Bindings.Count);
        globalNameOffsets = new List<uint>(Globals.Count);

        List<byte> tableBytes = new List<byte>();

        uint cursor = (uint)stringTableOffset;
        foreach (var binding in Bindings)
        {
            bindingNameOffsets.Add(cursor);
            byte[] nameBytes = Encoding.ASCII.GetBytes(binding.Name ?? string.Empty);
            tableBytes.AddRange(nameBytes);
            tableBytes.Add(0);
            cursor += (uint)nameBytes.Length + 1;
        }

        globalsStringOffset = cursor;
        foreach (var global in Globals)
        {
            globalNameOffsets.Add(cursor);
            byte[] nameBytes = Encoding.ASCII.GetBytes(global.Name ?? string.Empty);
            tableBytes.AddRange(nameBytes);
            tableBytes.Add(0);
            cursor += (uint)nameBytes.Length + 1;
        }

        stringTable = tableBytes.ToArray();
    }

    private static ShaderType ToShaderType(ShaderStageType stage)
    {
        return stage switch
        {
            ShaderStageType.Pixel => ShaderType.PixelShader,
            _ => ShaderType.VertexShader
        };
    }

    private static byte CalculateRegisterCount(uint size)
    {
        if (size == 0)
            return 0;

        uint count = (size + 15) / 16;
        return count > byte.MaxValue ? byte.MaxValue : (byte)count;
    }

    private static byte CalculateComponentCount(DxbcTypeDesc typeDesc)
    {
        uint rows = typeDesc.Rows == 0 ? 1u : typeDesc.Rows;
        uint columns = typeDesc.Columns == 0 ? 1u : typeDesc.Columns;

        if (rows > 1)
            return 4;

        return (byte)Math.Clamp((int)columns, 1, 4);
    }

    private static byte ClampToByte(uint value)
    {
        return value > byte.MaxValue ? byte.MaxValue : (byte)value;
    }
}
