using Avalonia.Controls;
using Avalonia.Markup.Xaml.Templates;

using System;
using System.ComponentModel;
using System.Reflection;

using static Volatility.Utilities.ClassUtilities;

namespace Vantage.ViewModels;

public partial class FieldViewModel : ViewModelBase, INotifyPropertyChanged
{
    private readonly object _instance;
    private readonly MemberInfo _memberInfo;
    private object _value;

    public string Name => _memberInfo.Name;

    public Type MemberType => _memberInfo switch
    {
        FieldInfo fieldInfo => fieldInfo.FieldType,
        PropertyInfo propertyInfo => propertyInfo.PropertyType
    };

    public string Category => GetAttribute<EditorCategoryAttribute>(_memberInfo)?.Category ?? string.Empty;
    public string Label => GetAttribute<EditorLabelAttribute>(_memberInfo)?.Label ?? _memberInfo.Name;
    public string Tooltip => GetAttribute<EditorTooltipAttribute>(_memberInfo)?.Tooltip ?? string.Empty;
    public bool IsHidden => GetAttribute<EditorHiddenAttribute>(_memberInfo) != null;

    public FieldViewModel(object instance, MemberInfo memberInfo)
    {
        _instance = instance;
        _memberInfo = memberInfo;
        _value = _memberInfo switch
        {
            FieldInfo fieldInfo => fieldInfo.GetValue(instance),
            PropertyInfo propertyInfo => propertyInfo.GetValue(instance),
        };        
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(Value));
    }

public object Value
{
    get
    {
        if (MemberType == typeof(string))
        {
            return _value?.ToString() ?? string.Empty;
        }
        return _value;
    }
    set
    {
        if (!Equals(value, _value))
        {
            try
            {
                object convertedValue = Convert.ChangeType(value, MemberType);
                switch (_memberInfo)
                {
                    case FieldInfo fieldInfo:
                        fieldInfo.SetValue(_instance, convertedValue);
                        break;
                    case PropertyInfo propertyInfo:
                        propertyInfo.SetValue(_instance, convertedValue);
                        break;
                }
                _value = convertedValue;
                OnPropertyChanged(nameof(Value));
            }
            catch (Exception)
            {
                // Handle conversion exceptions as needed
            }
        }
    }
}



    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public DataTemplate SelectedTemplate => GetTemplate(MemberType);

    public static DataTemplate GetTemplate(Type value)
    {
        if (value.IsClass)
        {
            return (DataTemplate)Avalonia.Application.Current.FindResource("ClassTemplate");
        }
        if (value == typeof(System.Boolean))
        {
            return (DataTemplate)Avalonia.Application.Current.FindResource("BooleanTemplate");
        }
        if (value.IsEnum)
        {
            return (DataTemplate)Avalonia.Application.Current.FindResource("EnumTemplate");
        }
        if (value == typeof(string) || value == typeof(int) || value == typeof(long) || value == typeof(short))
        {
            return (DataTemplate)Avalonia.Application.Current.FindResource("TextBoxTemplate");
        }
        
        return (DataTemplate)Avalonia.Application.Current.FindResource("TextBoxTemplate");
    }
}
