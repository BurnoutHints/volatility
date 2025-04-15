using System;
using System.Reflection;

using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

using Volatility.Utilities;

namespace Volatility.Utilities;

public class BinaryDataTypeConverter : IYamlTypeConverter
{
    public bool Accepts(Type type)
    {
        return type == typeof(byte[]);
    }

    public object ReadYaml(IParser parser, Type type, ObjectDeserializer nestedObjectDeserializer)
    {
        var value = parser.Current as Scalar;
        if (value == null || value.Value == "null")
        {
            return null;
        }

        try
        {
            byte[] compressedData = Convert.FromBase64String(value.Value);
            return ZLibUtilities.Decompress(compressedData);
        }
        catch (Exception)
        {
            return Convert.FromBase64String(value.Value);
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
            emitter.Emit(new Scalar(base64));
        }
        else
        {
            emitter.Emit(new Scalar(value.ToString()));
        }
    }
} 