using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Volatility.Utilities;

public class StringEnumYamlTypeConverter : IYamlTypeConverter
{
    public bool Accepts(Type type) => type.IsEnum;

    public object ReadYaml(IParser parser, Type type, ObjectDeserializer nestedObjectDeserializer)
    {
        var scalar = parser.Consume<Scalar>();
        return Enum.Parse(type, scalar.Value);
    }   

    public void WriteYaml(IEmitter emitter, object value, Type type, ObjectSerializer nestedObjectSerializer)
    {
        emitter.Emit(new Scalar(value.ToString()));
    }
}