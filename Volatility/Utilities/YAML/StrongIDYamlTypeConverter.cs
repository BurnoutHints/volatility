using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Volatility.Utilities
{
    public class StrongIDYamlTypeConverter : IYamlTypeConverter
    {
        public bool Accepts(Type type)
        {
            return type.IsValueType
                && type.IsGenericType
                && type.GetGenericTypeDefinition() == typeof(StrongID<>);
        }

        public object ReadYaml(IParser parser, Type type, ObjectDeserializer nestedObjectDeserializer)
        {
            var scalar = parser.Consume<Scalar>().Value!;
            var hex = scalar.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                ? scalar.Substring(2)
                : scalar;
            var value = Convert.ToUInt64(hex, 16);

            var ctor = type.GetConstructor(new[] { typeof(ulong) })
                       ?? throw new InvalidOperationException(
                            $"No ctor(ulong) found on {type.FullName}"
                          );
            return ctor.Invoke(new object[] { value })!;
        }

        public void WriteYaml(IEmitter emitter, object value, Type type, ObjectSerializer nestedObjectSerializer)
        {
            var text = value.ToString()!;
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
}
