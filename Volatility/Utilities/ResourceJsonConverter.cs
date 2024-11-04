using System.Reflection;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Volatility.Resources;

namespace Volatility.Utilities;

public class ResourceJsonConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return typeof(Resource).IsAssignableFrom(objectType);
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        JObject mainObject = new JObject();
        Type type = value.GetType();

        // Collect all class types from base to derived
        var classTypes = new List<Type>();
        Type currentType = type;
        while (currentType != null && CanConvert(currentType))
        {
            classTypes.Add(currentType);
            currentType = currentType.BaseType;
        }

        classTypes.Reverse(); // Process from base to derived

        HashSet<string> processedMembers = new HashSet<string>();

        // Process each class type
        foreach (var classType in classTypes)
        {
            JObject classProperties = new JObject();

            var members = classType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Cast<MemberInfo>()
                .Concat(classType.GetFields(BindingFlags.Public | BindingFlags.Instance))
                .ToList();

            foreach (var member in members)
            {
                if (!processedMembers.Contains(member.Name))
                {
                    if (member.MemberType == MemberTypes.Property)
                    {
                        classProperties.Add(member.Name, JToken.FromObject(((PropertyInfo)member).GetValue(value), serializer));
                    }
                    else if (member.MemberType == MemberTypes.Field)
                    {
                        classProperties.Add(member.Name, JToken.FromObject(((FieldInfo)member).GetValue(value), serializer));
                    }
                    processedMembers.Add(member.Name);
                }
            }

            if (classProperties.Count > 0)
            {
                string className = classType.Name;
                mainObject.Add($"{className}Properties", classProperties);
            }
        }

        writer.WriteRawValue(mainObject.ToString());
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        // TODO: Implement reader
        throw new NotImplementedException();
    }

    public static object DeserializeResource(Type resourceClass, string json)
    {
        JObject obj = JObject.Parse(json);

        JObject mergedProperties = new();

        List<string> sectionNames = [];

        Type currentType = resourceClass;

        while (currentType != null && currentType != typeof(object))
        {
            string sectionName = currentType.Name + "Properties";
            sectionNames.Insert(0, sectionName);
            currentType = currentType.BaseType;
        }

        foreach (string sectionName in sectionNames)
        {
            if (obj[sectionName] != null)
            {
                mergedProperties.Merge(obj[sectionName], new JsonMergeSettings
                {
                    MergeArrayHandling = MergeArrayHandling.Union
                });
            }
        }

        object resource = mergedProperties.ToObject(resourceClass);

        return resource;
    }

}