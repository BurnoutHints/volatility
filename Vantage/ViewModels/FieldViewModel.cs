using Avalonia.Controls;
using Avalonia.Markup.Xaml.Templates;

using System;
using System.ComponentModel;
using System.Reflection;

using static Volatility.Utilities.ClassUtilities;

namespace Vantage.ViewModels;

public class FieldViewModel : ViewModelBase, INotifyPropertyChanged
{
    private readonly object _instance;
    private readonly FieldInfo _fieldInfo;
    private object _value;

    public string Name => _fieldInfo.Name;

    public Type FieldType => _fieldInfo.FieldType;

    public string Category => GetAttribute<EditorCategoryAttribute>(_fieldInfo)?.Category ?? string.Empty;
    public string Label => GetAttribute<EditorLabelAttribute>(_fieldInfo)?.Label ?? _fieldInfo.Name;
    public string Tooltip => GetAttribute<EditorTooltipAttribute>(_fieldInfo)?.Tooltip ?? string.Empty;
    public bool IsHidden => GetAttribute<EditorHiddenAttribute>(_fieldInfo) != null;

    public FieldViewModel(object instance, FieldInfo fieldInfo)
    {
        _instance = instance;
        _fieldInfo = fieldInfo;
        _value = _fieldInfo.GetValue(_instance);
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(Value));
    }

    public string Value
    {
        get => Convert.ToString(_value);
        set
        {
            if (value != Convert.ToString(_value))
            {
                try
                {
                    object convertedValue = Convert.ChangeType(value, FieldType);
                    _fieldInfo.SetValue(_instance, convertedValue);
                    _value = convertedValue;
                    OnPropertyChanged(nameof(Value));
                }
                catch (Exception)
                {
                }
            }
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public DataTemplate SelectedTemplate => GetTemplate(_fieldInfo.FieldType);

    public DataTemplate GetTemplate(Type value)
    {
        if (value == typeof(bool))
        {
            return (DataTemplate)Avalonia.Application.Current.FindResource("BooleanTemplate");
        }
        if (value == typeof(Enum))
        {
            return (DataTemplate)Avalonia.Application.Current.FindResource("EnumTemplate");
        }
        if (value == typeof(string))
        {
            return (DataTemplate)Avalonia.Application.Current.FindResource("StringTemplate");
        }

        return (DataTemplate)Avalonia.Application.Current.FindResource("DefaultTemplate");
    }
}
