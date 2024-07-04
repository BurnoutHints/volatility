using System.Reflection;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Volatility.Resource.TextureHeader;

namespace Volatility.Utilities;

public class TextureHeaderJsonConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return typeof(TextureHeaderBase).IsAssignableFrom(objectType);
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        JObject mainObject = new JObject();
        Type type = value.GetType();

        var baseMembers = typeof(TextureHeaderBase).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Cast<MemberInfo>()
            .Concat(typeof(TextureHeaderBase).GetFields(BindingFlags.Public | BindingFlags.Instance))
            .ToList();

        // Base class members
        JObject baseProperties = new JObject();
        foreach (var member in baseMembers)
        {
            if (member.MemberType == MemberTypes.Property)
                baseProperties.Add(member.Name, JToken.FromObject(((PropertyInfo)member).GetValue(value), serializer));
            else if (member.MemberType == MemberTypes.Field)
                baseProperties.Add(member.Name, JToken.FromObject(((FieldInfo)member).GetValue(value), serializer));
        }
        mainObject.Add(type.BaseType.Name + "Properties", baseProperties);

        // Don't collect & serialize members that are in the base class
        if (type != typeof(TextureHeaderBase))
        {
            JObject derivedProperties = new JObject();
            var derivedMembers = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Cast<MemberInfo>()
                .Concat(type.GetFields(BindingFlags.Public | BindingFlags.Instance))
                .Where(dm => !baseMembers.Any(bm => bm.Name == dm.Name && bm.DeclaringType == typeof(TextureHeaderBase)))
                .ToList();

            foreach (var member in derivedMembers)
            {
                if (member.MemberType == MemberTypes.Property)
                    derivedProperties.Add(member.Name, JToken.FromObject(((PropertyInfo)member).GetValue(value), serializer));
                else if (member.MemberType == MemberTypes.Field)
                    derivedProperties.Add(member.Name, JToken.FromObject(((FieldInfo)member).GetValue(value), serializer));
            }
            if (derivedProperties.Count > 0)
            {
                mainObject.Add(type.Name + "Properties", derivedProperties);
            }
        }

        mainObject.WriteTo(writer);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        // TODO: Implement reader
        throw new NotImplementedException();
    }
}