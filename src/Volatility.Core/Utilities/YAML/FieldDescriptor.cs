using System;
using System.Reflection;

using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace Volatility.Utilities;

public class FieldDescriptor : IPropertyDescriptor
{
    private readonly FieldInfo fieldInfo;
    
    public FieldDescriptor(FieldInfo field)
    {
        fieldInfo = field;
        // Set defaults
        Required = false;
        TypeOverride = null;
        ConverterType = null;
    }
    
    public string Name
    {
        get => fieldInfo.Name;
        set { }
    }
    
    public Type Type => fieldInfo.FieldType;
    
    public int Order { get; set; }
    
    public ScalarStyle ScalarStyle { get; set; }
    
    public bool CanWrite => true;
    
    public bool AllowNulls => true;
    
    public Type? TypeOverride { get; set; }
    
    public bool Required { get; set; }
    
    public Type? ConverterType { get; set; }
    
    public T? GetCustomAttribute<T>() where T : Attribute
    {
        return fieldInfo.GetCustomAttribute<T>();
    }
    
    public void Write(object target, object? value)
    {
        fieldInfo.SetValue(target, value);
    }
    
    public IObjectDescriptor Read(object target)
    {
        var value = fieldInfo.GetValue(target);
        return new ObjectDescriptor(value, fieldInfo.FieldType, typeof(object));
    }
}
