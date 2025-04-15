using System;
using System.Reflection;

using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.TypeInspectors;

namespace Volatility.Utilities;

public class BinaryDataSerializer : IYamlTypeConverter
{
    public bool Accepts(Type type)
    {
        return type == typeof(byte[]);
    }
    
    public object ReadYaml(IParser parser, Type type, ObjectDeserializer nestedObjectDeserializer)
    {
        var scalar = parser.Consume<Scalar>();
        if (scalar == null || scalar.Value == "null")
        {
            return null;
        }

        byte[] decodedBytes;
        try
        {
            decodedBytes = Convert.FromBase64String(scalar.Value);
        }
        catch (FormatException ex)
        {
            Console.WriteLine($"Error decoding Base64: {ex.Message}");
            return Array.Empty<byte>(); 
        }

        if (scalar.Tag == "!!binary" || scalar.Tag == "tag:yaml.org,2002:binary")
        {
            try
            {
                return ZLibUtilities.Decompress(decodedBytes);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error decompressing data: {ex.Message}. Returning raw decoded bytes.");
                return decodedBytes; 
            }
        }
        else
        {
            Console.WriteLine("Warning: Received byte[] data without !!binary tag. Returning raw decoded bytes.");
            return decodedBytes;
        }
    }
    
    public void WriteYaml(IEmitter emitter, object value, Type type, ObjectSerializer nestedObjectSerializer)
    {
        if (value == null)
        {
            emitter.Emit(new Scalar("null"));
            return;
        }

        if (value is byte[] bytes)
        {
            byte[] compressed = ZLibUtilities.Compress(bytes);
            string base64 = Convert.ToBase64String(compressed);
            emitter.Emit(new Scalar(AnchorName.Empty, new TagName("!!binary"), base64));
        }
        else
        {
            emitter.Emit(new Scalar(value.ToString()));
        }
    }
}