using System;
using System.Reflection;

using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace Volatility.Utilities;

public class FieldDescriptor : IPropertyDescriptor
{
    private readonly FieldInfo field;
    
    public FieldDescriptor(FieldInfo field)
    {
        this.field = field;
        // Set defaults
        Required = false;
        TypeOverride = null;
        ConverterType = null;
    }
    
    public string Name
    {
        get => field.Name;
        set { }
    }
    
    public Type Type => field.FieldType;
    
    public int Order { get; set; }
    
    public ScalarStyle ScalarStyle { get; set; }
    
    public bool CanWrite => true;
    
    public bool AllowNulls => true;
    
    public Type TypeOverride { get; set; }
    
    public bool Required { get; set; }
    
    public Type ConverterType { get; set; }
    
    public T GetCustomAttribute<T>() where T : Attribute
    {
        return field.GetCustomAttribute<T>();
    }
    
    public void Write(object target, object value)
    {
        field.SetValue(target, value);
    }
    
    public IObjectDescriptor Read(object target)
    {
        var value = field.GetValue(target);
        return new ObjectDescriptor(value, field.FieldType, typeof(object));
    }
}