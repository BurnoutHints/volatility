using System;
using System.Text;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NodeDeserializers;

namespace Volatility.Utilities;

public class BinaryNodeDeserializer : INodeDeserializer
{
    public bool Deserialize(
        IParser parser,
        Type expectedType,
        Func<IParser, Type, object> nestedObjectDeserializer,
        out object value,
        ObjectDeserializer objectDeserializer)
    {
        if (parser.Current is Scalar scalar && (scalar.Tag == "tag:yaml.org,2002:binary" || scalar.Tag == "!!binary"))
        {
            try
            {
                byte[] compressedData = Convert.FromBase64String(scalar.Value);
                byte[] decompressed = ZLibUtilities.Decompress(compressedData);
                
                if (expectedType == typeof(byte[]))
                {
                    value = decompressed;
                }
                else if (expectedType == typeof(string))
                {
                    value = Encoding.UTF8.GetString(decompressed);
                }
                else
                {
                    value = null;
                    return false;
                }
                
                parser.MoveNext();
                return true;
            }
            catch
            {
                byte[] rawBytes = Convert.FromBase64String(scalar.Value);
                
                if (expectedType == typeof(byte[]))
                {
                    value = rawBytes;
                }
                else if (expectedType == typeof(string))
                {
                    value = Encoding.UTF8.GetString(rawBytes);
                }
                else
                {
                    value = null;
                    return false;
                }
                
                parser.MoveNext();
                return true;
            }
        }
        
        value = null;
        return false;
    }
}