using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Volatility.Utilities;

public class ResourceIDYamlTypeConverter : IYamlTypeConverter
{
    public bool Accepts(Type type) => type == typeof(ResourceID);

    public object ReadYaml(IParser parser, Type type, ObjectDeserializer nestedObjectDeserializer)
    {
        var scalar = parser.Consume<Scalar>().Value;
        var hex = scalar.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                   ? scalar.Substring(2)
                   : scalar;
        var ul = Convert.ToUInt64(hex, 16);
        return new ResourceID(ul);
    }

    public void WriteYaml(IEmitter emitter, object value, Type type, ObjectSerializer nestedObjectSerializer)
    {
        var text = value.ToString();
        emitter.Emit(new Scalar(
            anchor: null,
            tag: null,
            value: text,
            style: ScalarStyle.Plain,
            isPlainImplicit: true,
            isQuotedImplicit: false
        ));
    }
}