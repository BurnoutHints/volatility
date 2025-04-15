using System;
using System.Text;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Volatility.Utilities;

public class BinaryTypeConverter : IYamlTypeConverter
{
    public bool Accepts(Type type)
    {
        return type == typeof(byte[]) || type == typeof(string);
    }
    
    public object ReadYaml(IParser parser, Type type, ObjectDeserializer nestedObjectDeserializer)
    {
        if (!(parser.Current is Scalar scalar))
        {
            throw new YamlException(parser.Current.Start, parser.Current.End, 
                $"Expected a scalar for !!binary data but encountered {parser.Current.GetType().Name}.");
        }
        
        if (scalar.Tag != "tag:yaml.org,2002:binary" && scalar.Tag != "!!binary")
        {
            if (type == typeof(string))
            {
                return scalar.Value;
            }
            else
            {
                throw new FormatException("Expected a !!binary tag for binary conversion.");
            }
        }
        
        byte[] rawBytes = Convert.FromBase64String(scalar.Value);
        byte[] decompressed;
        
        try
        {
            decompressed = ZLibUtilities.Decompress(rawBytes);
        }
        catch
        {
            decompressed = rawBytes;
        }
        
        parser.MoveNext();
        if (type == typeof(byte[]))
        {
            return decompressed;
        }
        else if (type == typeof(string))
        {
            return Encoding.UTF8.GetString(decompressed);
        }
        
        throw new NotSupportedException("Type not supported by BinaryTypeConverter");
    }
    
    public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer nestedObjectSerializer)
    {
        if (type == typeof(byte[]))
        {
            byte[] data = (byte[])value!;
            byte[] compressed = ZLibUtilities.Compress(data);
            string base64 = Convert.ToBase64String(compressed);
            emitter.Emit(new Scalar(null, "!!binary", base64, ScalarStyle.Plain, isPlainImplicit: true, isQuotedImplicit: false));
        }
        else if (type == typeof(string))
        {
            byte[] data = Encoding.UTF8.GetBytes((string)value!);
            byte[] compressed = ZLibUtilities.Compress(data);
            string base64 = Convert.ToBase64String(compressed);
            emitter.Emit(new Scalar(null, "!!binary", base64, ScalarStyle.Plain, isPlainImplicit: true, isQuotedImplicit: false));
        }
        else
        {
            throw new NotSupportedException("Type not supported by BinaryTypeConverter");
        }
    }
}
