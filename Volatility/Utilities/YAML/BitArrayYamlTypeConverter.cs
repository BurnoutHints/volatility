using System.Collections;
using System.Text;

using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Volatility.Utilities;

public class BitArrayYamlTypeConverter : IYamlTypeConverter
{
    public bool Accepts(Type type) => type == typeof(BitArray);

    public object ReadYaml(IParser parser, Type type, ObjectDeserializer nestedObjectDeserializer)
    {
        string bits = parser.Consume<Scalar>().Value ?? string.Empty;
        BitArray bitArray = new(bits.Length);

        for (int i = 0; i < bits.Length; i++)
        {
            bitArray[i] = bits[i] switch
            {
                '0' => false,
                '1' => true,
                _ => throw new YamlException($"Invalid BitArray value '{bits[i]}'. Expected only '0' or '1'."),
            };
        }

        return bitArray;
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer nestedObjectSerializer)
    {
        if (value is not BitArray bitArray)
        {
            emitter.Emit(new Scalar(string.Empty));
            return;
        }

        StringBuilder bits = new(bitArray.Length);

        foreach (bool bit in bitArray)
        {
            bits.Append(bit ? '1' : '0');
        }

        emitter.Emit(new Scalar(bits.ToString()));
    }
}
